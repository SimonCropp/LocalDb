using MethodTimer;

class Wrapper : IDisposable
{
    public readonly string Directory;
    ushort size;
    ushort shutdownTimeout;
    Func<SqlConnection, Task>? callback;
    SemaphoreSlim semaphoreSlim = new(1, 1);
    SemaphoreSlim sharedLock = new(1, 1);
    bool sharedCreated;
    SemaphoreSlim poolLock = new(1, 1);
    SemaphoreSlim? poolLease;
    ConcurrentQueue<string> poolAvailable = new();
    bool poolCreated;
    Task? poolFill;
    volatile Exception? poolFillException;
    public readonly string MasterConnectionString;
    string instance;
    public readonly string DataFile;
    public readonly string LogFile;
    public readonly string TemplateConnectionString;
    public readonly string ServerName;
    Task startupTask = null!;
    bool templateProvided;

    public Wrapper(
        string instance,
        string directory,
        ushort size = 3,
        ExistingTemplate? existingTemplate = null,
        Func<SqlConnection, Task>? callback = null,
        ushort? shutdownTimeout = null)
    {
        Guard.AgainstBadOS();
        Guard.AgainstDatabaseSize(size);
        Guard.AgainstInvalidFileName(instance);

        LocalDbLogging.WrapperCreated = true;
        this.instance = instance;
        MasterConnectionString = LocalDbSettings.BuildConnectionString(instance, "master", true);
        TemplateConnectionString = LocalDbSettings.BuildConnectionString(instance, "template", false);
        Directory = directory;

        LocalDbLogging.LogIfVerbose($"Directory: {directory}");
        this.size = size;
        this.shutdownTimeout = shutdownTimeout ?? LocalDbSettings.ShutdownTimeout;
        Guard.AgainstZeroShutdownTimeout(this.shutdownTimeout);
        this.callback = callback;
        if (existingTemplate is null)
        {
            templateProvided = false;
            DataFile = Path.Combine(directory, "template.mdf");
            LogFile = Path.Combine(directory, "template_log.ldf");
        }
        else
        {
            templateProvided = true;
            DataFile = existingTemplate.Value.DataPath;
            LogFile = existingTemplate.Value.LogPath;
        }

        var directoryInfo = System.IO.Directory.CreateDirectory(directory);
        directoryInfo.ResetAccess();

        ServerName = $@"(LocalDb)\{instance}";
    }

    [Time("Name: '{name}'")]
    public async Task<SqlConnection> CreateDatabaseFromTemplate(string name)
    {
        if (string.Equals(name, "template", StringComparison.OrdinalIgnoreCase))
        {
            throw new("The database name 'template' is reserved.");
        }

        if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new ArgumentException($"Invalid database name. Name must be valid to use as a file name. Value: {name}", nameof(name));
        }

        // Explicitly dont take offline here, since that is done at startup
        var dataFile = Path.Combine(Directory, $"{name}.mdf");
        var logFile = Path.Combine(Directory, $"{name}_log.ldf");

        var createOrMakeOnlineCommand = SqlBuilder.GetCreateOrMakeOnlineCommand(name, dataFile, logFile);
        var connectionString = LocalDbSettings.BuildConnectionString(instance, name, false);

        await startupTask;

#if NET5_0_OR_GREATER
        await using (var masterConnection = await OpenMasterConnection())
#else
        using (var masterConnection = await OpenMasterConnection())
#endif
        {
            await masterConnection.ExecuteCommandAsync(SqlBuilder.GetTakeDbsOfflineCommand(name));

            // Copy data and log files in parallel for better performance
            await Task.WhenAll(
                File.CopyAsync(DataFile, dataFile),
                File.CopyAsync(LogFile, logFile));

            FileExtensions.MarkFileAsWritable(dataFile);
            FileExtensions.MarkFileAsWritable(logFile);

            await masterConnection.ExecuteCommandAsync(createOrMakeOnlineCommand);
        }

        var resultConnection = new SqlConnection(connectionString);
        await resultConnection.OpenAsync();
        return resultConnection;
    }

    public void Start(DateTime timestamp, Func<SqlConnection, Task> buildTemplate)
    {
#if RELEASE
        try
        {
#endif
        var stopwatch = Stopwatch.StartNew();
        InnerStart(timestamp, buildTemplate);
        var message = $"Start `{ServerName}` {stopwatch.ElapsedMilliseconds}ms.";

        LocalDbLogging.Log(message);
#if RELEASE
        }
        catch (Exception exception)
        {
            throw ExceptionBuilder.WrapLocalDbFailure(instance, Directory, exception);
        }
#endif
    }

    // Must live on this non-generic type: a lambda inside SqlInstance<T> compiles into a
    // closure class generic over T, and executing it resolves T's type handle. When the
    // SqlInstance constructor runs inside a module or static initializer of T's assembly,
    // that resolution on a thread-pool thread blocks on the initializer lock, and the
    // constructor joining this task before returning turns that into a deadlock.
    public Task StartOnThreadPool(DateTime timestamp, Func<SqlConnection, Task> buildTemplate) =>
        Task.Run(() => Start(timestamp, buildTemplate));

    public Task AwaitStart() => startupTask;

    public async Task<SqlConnection> OpenExistingDatabase(string name, bool pool = false)
    {
        await startupTask;
        var connectionString = LocalDbSettings.BuildConnectionString(instance, name, pool);
        var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        return connection;
    }

    /// <summary>
    /// Leases one database from the pool. The caller must call <see cref="ReleasePooled" />
    /// with the returned name once finished, otherwise the pool will starve.
    /// </summary>
    public async Task<(SqlConnection Connection, string Name)> OpenPooledDatabase(
        Func<SqlConnection, Task>? initialize = null)
    {
        // Double-checked pattern, as per OpenSharedDatabase: once the pool exists the lock is
        // no longer needed and the common case skips the semaphore entirely.
        if (!Volatile.Read(ref poolCreated))
        {
            await poolLock.WaitAsync();
            try
            {
                if (!poolCreated)
                {
                    await CreatePool(initialize);
                    Volatile.Write(ref poolCreated, true);
                }
            }
            finally
            {
                poolLock.Release();
            }
        }

        ThrowIfPoolFillFailed();

        // Blocks once every database built so far is leased, which is what bounds pooled
        // concurrency to PoolSize. Early in a run it can also block on a database the
        // background fill has not produced yet.
        await poolLease!.WaitAsync();

        if (!poolAvailable.TryDequeue(out var name))
        {
            // Put the permit back so the next caller reaches the same conclusion rather than
            // waiting on a database that will never arrive.
            poolLease.Release();
            ThrowIfPoolFillFailed();
            throw new("Pooled database lease was acquired but no database was available. This indicates a release was missed.");
        }

        // Connection pooling is on for pooled databases: they are opened and closed once per
        // test, so returning the connection to the ADO.NET pool avoids a physical reconnect
        // every time. auto_close is turned off when the pool is built, so the database itself
        // also stays up between leases.
        var connection = await OpenExistingDatabase(name, pool: true);
        return (connection, name);
    }

    /// <summary>
    /// Returns a database leased by <see cref="OpenPooledDatabase" /> to the pool.
    /// </summary>
    public void ReleasePooled(string name)
    {
        poolAvailable.Enqueue(name);
        poolLease!.Release();
    }

    async Task CreatePool(Func<SqlConnection, Task>? initialize)
    {
        var size = LocalDbSettings.PoolSize;
        // Guarded here rather than in the setter, matching AgainstZeroShutdownTimeout: a zero
        // would otherwise surface as an ArgumentOutOfRangeException from SemaphoreSlim on the
        // first pooled test, with nothing naming PoolSize as the cause.
        Guard.AgainstZeroPoolSize(size);

        // Starts empty and gains a permit as each database lands, rather than starting full.
        // That is what lets a lease be served off the databases that exist so far, instead of
        // every lease waiting for the whole pool.
        poolLease = new(0, size);

        // Only the first database is awaited, so the first pooled test pays one file copy and
        // attach rather than PoolSize of them. The rest fill in on a background task while
        // that test runs, and a test needing more concurrency than is built yet simply waits
        // on poolLease until the next database lands.
        await AddPooled(1, initialize);

        poolFill = Task.Run(() => FillPool(size, initialize));
    }

    async Task FillPool(ushort size, Func<SqlConnection, Task>? initialize)
    {
        var index = 2;
        try
        {
            // Deliberately serial: the fill now runs alongside tests, and PoolSize concurrent
            // file copies would compete with them for the same disk.
            for (; index <= size; index++)
            {
                await AddPooled(index, initialize);
            }
        }
        catch (Exception exception)
        {
            // A background failure must not leave leases waiting on databases that will never
            // arrive. Record the cause first, then release one permit per unbuilt database so
            // every waiter wakes, finds nothing to dequeue, and rethrows that cause. This can
            // never exceed the semaphore maximum: index - 1 databases were built, so at most
            // index - 1 permits are outstanding.
            poolFillException = exception;
            poolLease!.Release(size - index + 1);
            return;
        }

        LocalDbLogging.LogIfVerbose($"Created pool of {size} databases");
    }

    async Task AddPooled(int index, Func<SqlConnection, Task>? initialize)
    {
        var name = $"Pooled{index}";
        var connection = await CreateDatabaseFromTemplate(name);
        try
        {
            // Attach resets auto_close to the model default (on), under which the database
            // shuts down whenever its last connection closes and the next lease pays a full
            // database startup. Pooled databases are reopened once per test for the lifetime
            // of the run, so that cycle would recur constantly.
            await connection.ExecuteCommandAsync($"alter database [{name}] set auto_close off;");

            if (initialize != null)
            {
                await initialize(connection);
            }
        }
        finally
        {
            // Also on the failure path, so a fill that dies part way does not strand a
            // connection against a database no one will ever lease.
#if NET5_0_OR_GREATER
            await connection.DisposeAsync();
#else
            connection.Dispose();
#endif
        }

        // Enqueue before releasing, so a waiter woken by the release always finds a database.
        poolAvailable.Enqueue(name);
        poolLease!.Release();
    }

    void ThrowIfPoolFillFailed()
    {
        var exception = poolFillException;
        if (exception != null)
        {
            throw new("Failed to build the pooled databases in the background.", exception);
        }
    }

    // Teardown deletes the instance directory, so a background fill still copying files into
    // it has to finish first. FillPool swallows its own failure, so this cannot throw.
    void WaitForPoolFill() => poolFill?.GetAwaiter().GetResult();

    public async Task<SqlConnection> OpenSharedDatabase(
        Func<SqlConnection, Task>? initialize = null)
    {
        // Double-checked pattern: once the Shared DB is created the lock is no longer needed,
        // and the common case (after first call) skips the semaphore entirely.
        if (!Volatile.Read(ref sharedCreated))
        {
            await sharedLock.WaitAsync();
            try
            {
                if (!sharedCreated)
                {
                    var initConnection = await CreateDatabaseFromTemplate("Shared");

                    // Attach resets auto_close to the model default (on), under which the database
                    // cleanly shuts down whenever its last connection closes and the next
                    // connection pays a full database startup. Shared-database connections are
                    // opened with pooling disabled and tests using it need not overlap, so that
                    // close/reopen cycle recurs for the lifetime of the run. Benchmark
                    // (AutoCloseBenchmarks): open-query-close cycles are ~6x faster with
                    // auto_close off. Per-test databases are left as-is: they hold a single
                    // connection for their whole life (never reopen), and auto_close lets the
                    // instance release their memory once disposed.
                    await initConnection.ExecuteCommandAsync("alter database [Shared] set auto_close off;");

                    if (initialize != null)
                    {
                        await initialize(initConnection);
                    }

#if NET5_0_OR_GREATER
                    await initConnection.DisposeAsync();
#else
                    initConnection.Dispose();
#endif
                    Volatile.Write(ref sharedCreated, true);
                }
            }
            finally
            {
                sharedLock.Release();
            }
        }

        return await OpenExistingDatabase("Shared");
    }

    void InnerStart(DateTime timestamp, Func<SqlConnection, Task> buildTemplate)
    {
        void CleanStart()
        {
            FileExtensions.CleanDirectory(Directory);
            LocalDbApi.CreateInstance(instance);
            LocalDbApi.StartInstance(instance);
            startupTask = CreateAndDetachTemplate(
                timestamp,
                buildTemplate,
                rebuildTemplate: true,
                optimizeModelDb: true);
        }

        var info = LocalDbApi.GetInstance(instance);

        if (!info.Exists)
        {
            CleanStart();
            return;
        }

        if (!info.IsRunning)
        {
            // Instead of deleting and recreating, just start the stopped instance.
            // This preserves the existing template files on disk and allows
            // warm/rebuild scenarios instead of always doing a cold start.
            LocalDbLogging.LogIfVerbose("LocalDb not running. So start and respect timestamp checks");
            LocalDbApi.StartInstance(instance);
            // Fall through to the data file and timestamp checks below
        }

        if (!File.Exists(DataFile))
        {
            LocalDbApi.StopAndDelete(instance);
            CleanStart();
            return;
        }

        var templateLastMod = File.GetCreationTime(DataFile);
        if (timestamp == templateLastMod)
        {
            LocalDbLogging.LogIfVerbose("Not modified so skipping rebuild");
            startupTask = CreateAndDetachTemplate(timestamp, buildTemplate, false, false);
        }
        else
        {
            startupTask = CreateAndDetachTemplate(timestamp, buildTemplate, true, false);
        }
    }

    [Time("Timestamp: '{timestamp}', RebuildTemplate: '{rebuildTemplate}', OptimizeModelDb: '{optimizeModelDb}'")]
    async Task CreateAndDetachTemplate(
        DateTime timestamp,
        Func<SqlConnection, Task> buildTemplate,
        bool rebuildTemplate,
        bool optimizeModelDb)
    {
#if NET5_0_OR_GREATER
        await using var masterConnection = await OpenMasterConnection();
#else
        using var masterConnection = await OpenMasterConnection();
#endif

        LocalDbLogging.LogIfVerbose($"SqlServerVersion: {masterConnection.ServerVersion}");

        if (optimizeModelDb)
        {
            await masterConnection.ExecuteCommandAsync(SqlBuilder.GetOptimizeModelDbCommand(size, shutdownTimeout));
        }

        if (rebuildTemplate && !templateProvided)
        {
            await Rebuild(timestamp, buildTemplate, masterConnection);
        }
        else
        {
            if (callback != null)
            {
                // Attach the template database temporarily to run the callback
                await masterConnection.ExecuteCommandAsync(SqlBuilder.GetAttachTemplateCommand(DataFile, LogFile));

#if NET5_0_OR_GREATER
                await using (var connection = new SqlConnection(TemplateConnectionString))
#else
                using (var connection = new SqlConnection(TemplateConnectionString))
#endif
                {
                    await connection.OpenAsync();
                    await callback(connection);
                    await connection.ExecuteCommandAsync("checkpoint");
                }

                // Apply template settings then detach in a single batch.
                await masterConnection.ExecuteCommandAsync(
                    SqlBuilder.TemplateSettingsCommand,
                    SqlBuilder.DetachTemplateCommand);
            }
        }
    }

    async Task<SqlConnection> OpenMasterConnection()
    {
        var connection = new SqlConnection(MasterConnectionString);
        await connection.OpenAsync();
        return connection;
    }

    async Task Rebuild(DateTime timestamp, Func<SqlConnection, Task> buildTemplate, SqlConnection masterConnection)
    {
        DeleteTemplateFiles();
        await masterConnection.ExecuteCommandAsync(SqlBuilder.GetCreateTemplateCommand(DataFile, LogFile));

        FileExtensions.MarkFileAsWritable(DataFile);
        FileExtensions.MarkFileAsWritable(LogFile);

#if NET5_0_OR_GREATER
        await using (var connection = new SqlConnection(TemplateConnectionString))
#else
        using (var connection = new SqlConnection(TemplateConnectionString))
#endif
        {
            await connection.OpenAsync();
            await buildTemplate(connection);
            if (callback != null)
            {
                await callback(connection);
            }

            await connection.ExecuteCommandAsync("checkpoint");
        }

        await masterConnection.ExecuteCommandAsync(
            SqlBuilder.TemplateSettingsCommand,
            SqlBuilder.DetachAndShrinkTemplateCommand);

        File.SetCreationTime(DataFile, timestamp);
    }

    [Time]
    public void DeleteInstance(ShutdownMode mode = ShutdownMode.KillProcess)
    {
        WaitForPoolFill();
        LocalDbApi.StopAndDelete(instance, mode);
        DirectoryFinder.DeleteInstance(instance);
        DeleteDirectory();
        Dispose();
    }

    [Time]
    public void DeleteInstance(ShutdownMode mode, TimeSpan timeout)
    {
        WaitForPoolFill();
        LocalDbApi.StopAndDelete(instance, mode, timeout);
        DirectoryFinder.DeleteInstance(instance);
        DeleteDirectory();
        Dispose();
    }

    void DeleteDirectory()
    {
        if (System.IO.Directory.Exists(Directory))
        {
            System.IO.Directory.Delete(Directory, true);
        }
    }

    void DeleteTemplateFiles()
    {
        if (File.Exists(DataFile))
        {
            File.Delete(DataFile);
        }

        if (File.Exists(LogFile))
        {
            File.Delete(LogFile);
        }
    }

    [Time("dbName: '{dbName}'")]
    public async Task DeleteDatabase(string dbName)
    {
        var commandText = SqlBuilder.BuildDeleteDbCommand(dbName);
#if NET5_0_OR_GREATER
        await using var connection = await OpenMasterConnection();
#else
        using var connection = await OpenMasterConnection();
#endif
        await connection.ExecuteCommandAsync(commandText);
        var dataFile = Path.Combine(Directory, $"{dbName}.mdf");
        var logFile = Path.Combine(Directory, $"{dbName}_log.ldf");
        File.Delete(dataFile);
        File.Delete(logFile);
    }

    [Time("dbName: '{dbName}'")]
    public async Task TakeOffline(string dbName)
    {
#if NET5_0_OR_GREATER
        await using var connection = await OpenMasterConnection();
#else
        using var connection = await OpenMasterConnection();
#endif
        await connection.ExecuteCommandAsync(SqlBuilder.GetTakeDbsOfflineCommand(dbName));
    }

    public void Dispose() => semaphoreSlim.Dispose();
}

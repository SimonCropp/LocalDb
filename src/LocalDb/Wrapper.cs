using MethodTimer;

class Wrapper : IDisposable
{
    public readonly string Directory;
    ushort size;
    Func<SqlConnection, Cancel, Task>? callback;
    SemaphoreSlim semaphoreSlim = new(1, 1);
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
        Func<SqlConnection, Cancel, Task>? callback = null)
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
    public async Task<SqlConnection> CreateDatabaseFromTemplate(string name, Cancel cancel = default)
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
        await using (var masterConnection = await OpenMasterConnection(cancel))
#else
        using (var masterConnection = await OpenMasterConnection(cancel))
#endif
        {
            await masterConnection.ExecuteCommandAsync(SqlBuilder.GetTakeDbsOfflineCommand(name), cancel);

            // Copy data and log files in parallel for better performance
            await Task.WhenAll(
                File.CopyAsync(DataFile, dataFile, cancel),
                File.CopyAsync(LogFile, logFile, cancel));

            FileExtensions.MarkFileAsWritable(dataFile);
            FileExtensions.MarkFileAsWritable(logFile);

            await masterConnection.ExecuteCommandAsync(createOrMakeOnlineCommand, cancel);
        }

        var resultConnection = new SqlConnection(connectionString);
        await resultConnection.OpenAsync(cancel);
        return resultConnection;
    }

    public void Start(DateTime timestamp, Func<SqlConnection, Cancel, Task> buildTemplate)
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

    public Task AwaitStart() => startupTask;

    void InnerStart(DateTime timestamp, Func<SqlConnection, Cancel, Task> buildTemplate)
    {
        void CleanStart()
        {
            FileExtensions.FlushDirectory(Directory);
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
            LocalDbApi.DeleteInstance(instance);
            CleanStart();
            return;
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
        Func<SqlConnection, Cancel, Task> buildTemplate,
        bool rebuildTemplate,
        bool optimizeModelDb,
        Cancel cancel = default)
    {
#if NET5_0_OR_GREATER
        await using var masterConnection = await OpenMasterConnection(cancel);
#else
        using var masterConnection = await OpenMasterConnection(cancel);
#endif

        LocalDbLogging.LogIfVerbose($"SqlServerVersion: {masterConnection.ServerVersion}");

        if (optimizeModelDb)
        {
            await masterConnection.ExecuteCommandAsync(SqlBuilder.GetOptimizeModelDbCommand(size), cancel);
        }

        if (rebuildTemplate && !templateProvided)
        {
            await Rebuild(timestamp, buildTemplate, masterConnection, cancel);
        }
        else
        {
            if (callback != null)
            {
                // Attach the template database temporarily to run the callback
                await masterConnection.ExecuteCommandAsync(SqlBuilder.GetAttachTemplateCommand(DataFile, LogFile), cancel);

#if NET5_0_OR_GREATER
                await using (var connection = new SqlConnection(TemplateConnectionString))
#else
                using (var connection = new SqlConnection(TemplateConnectionString))
#endif
                {
                    await connection.OpenAsync(cancel);
                    await callback(connection, cancel);
                }

                // Detach the template database after callback completes
                await masterConnection.ExecuteCommandAsync(SqlBuilder.DetachTemplateCommand, cancel);
            }
        }
    }

    async Task<SqlConnection> OpenMasterConnection(Cancel cancel = default)
    {
        var connection = new SqlConnection(MasterConnectionString);
        await connection.OpenAsync(cancel);
        return connection;
    }

    async Task Rebuild(DateTime timestamp, Func<SqlConnection, Cancel, Task> buildTemplate, SqlConnection masterConnection, Cancel cancel = default)
    {
        DeleteTemplateFiles();
        await masterConnection.ExecuteCommandAsync(SqlBuilder.GetCreateTemplateCommand(DataFile, LogFile), cancel);

        FileExtensions.MarkFileAsWritable(DataFile);
        FileExtensions.MarkFileAsWritable(LogFile);

#if NET5_0_OR_GREATER
        await using (var connection = new SqlConnection(TemplateConnectionString))
#else
        using (var connection = new SqlConnection(TemplateConnectionString))
#endif
        {
            await connection.OpenAsync(cancel);
            await buildTemplate(connection, cancel);
            if (callback != null)
            {
                await callback(connection, cancel);
            }
        }

        await masterConnection.ExecuteCommandAsync(SqlBuilder.DetachAndShrinkTemplateCommand, cancel);

        File.SetCreationTime(DataFile, timestamp);
    }

    [Time]
    public void DeleteInstance(ShutdownMode mode = ShutdownMode.KillProcess)
    {
        LocalDbApi.StopAndDelete(instance, mode);
        DeleteDirectory();
        Dispose();
    }

    [Time]
    public void DeleteInstance(ShutdownMode mode, TimeSpan timeout)
    {
        LocalDbApi.StopAndDelete(instance, mode, timeout);
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
    public async Task DeleteDatabase(string dbName, Cancel cancel = default)
    {
        var commandText = SqlBuilder.BuildDeleteDbCommand(dbName);
#if NET5_0_OR_GREATER
        await using var connection = await OpenMasterConnection(cancel);
#else
        using var connection = await OpenMasterConnection(cancel);
#endif
        await connection.ExecuteCommandAsync(commandText, cancel);
        var dataFile = Path.Combine(Directory, $"{dbName}.mdf");
        var logFile = Path.Combine(Directory, $"{dbName}_log.ldf");
        File.Delete(dataFile);
        File.Delete(logFile);
    }

    public void Dispose() => semaphoreSlim.Dispose();
}

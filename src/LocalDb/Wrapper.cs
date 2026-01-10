using MethodTimer;

class Wrapper : IDisposable
{
    public readonly string Directory;
    ushort size;
    Func<DbConnection, Task>? callback;
    SemaphoreSlim semaphoreSlim = new(1, 1);
    public readonly string MasterConnectionString;
    Func<string, DbConnection> buildConnection;
    string instance;
    public readonly string DataFile;
    public readonly string LogFile;
    public readonly string TemplateConnectionString;
    public readonly string ServerName;
    Task startupTask = null!;
    bool templateProvided;

    public Wrapper(
        Func<string, DbConnection> buildConnection,
        string instance,
        string directory,
        ushort size = 3,
        ExistingTemplate? existingTemplate = null,
        Func<DbConnection, Task>? callback = null)
    {
        Guard.AgainstBadOS();
        Guard.AgainstDatabaseSize(size);
        Guard.AgainstInvalidFileName(instance);

        LocalDbLogging.WrapperCreated = true;
        this.buildConnection = buildConnection;
        this.instance = instance;
        MasterConnectionString = LocalDbSettings.connectionBuilder(instance, "master");
        TemplateConnectionString = LocalDbSettings.connectionBuilder(instance, "template");
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
    public async Task<(string ConnectionString, DbConnection Connection)> CreateDatabaseFromTemplate(string name)
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
        var takeDbsOfflineCommand = SqlBuilder.GetTakeDbsOfflineCommand(name);
        var createOrMakeOnlineCommand = SqlBuilder.GetCreateOrMakeOnlineCommand(name, dataFile, logFile);

        await startupTask;

        var masterConnection = await OpenMasterConnection();
        await masterConnection.ExecuteCommandAsync(takeDbsOfflineCommand);

        await FileExtensions.CopyFileAsync(DataFile, dataFile);
        await FileExtensions.CopyFileAsync(LogFile, logFile);

        FileExtensions.MarkFileAsWritable(dataFile);
        FileExtensions.MarkFileAsWritable(logFile);

        await masterConnection.ExecuteCommandAsync(createOrMakeOnlineCommand);

        masterConnection.ChangeDatabase(name);

        return (LocalDbSettings.connectionBuilder(instance, name), masterConnection);
    }

    public void Start(DateTime timestamp, Func<DbConnection, Task> buildTemplate)
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

    void InnerStart(DateTime timestamp, Func<DbConnection, Task> buildTemplate)
    {
        void CleanStart()
        {
            FileExtensions.FlushDirectory(Directory);
            LocalDbApi.CreateInstance(instance);
            LocalDbApi.StartInstance(instance);
            startupTask = CreateAndDetachTemplate(
                timestamp,
                buildTemplate,
                rebuild: true,
                optimize: true);
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

    [Time("Timestamp: '{timestamp}', Rebuild: '{rebuild}', Optimize: '{optimize}'")]
    async Task CreateAndDetachTemplate(
        DateTime timestamp,
        Func<DbConnection, Task> buildTemplate,
        bool rebuild,
        bool optimize)
    {
#if NET5_0_OR_GREATER
        await using var masterConnection = await OpenMasterConnection();
#else
        using var masterConnection = await OpenMasterConnection();
#endif

        LocalDbLogging.LogIfVerbose($"SqlServerVersion: {masterConnection.ServerVersion}");

        if (optimize)
        {
            await masterConnection.ExecuteCommandAsync(SqlBuilder.GetOptimizationCommand(size));
        }

        if (rebuild && !templateProvided)
        {
            await Rebuild(timestamp, buildTemplate, masterConnection);
        }
    }

    async Task<DbConnection> OpenMasterConnection()
    {
        var connection = buildConnection(MasterConnectionString);
        await connection.OpenAsync();
        return connection;
    }

    async Task Rebuild(DateTime timestamp, Func<DbConnection, Task> buildTemplate, DbConnection masterConnection)
    {
        DeleteTemplateFiles();
        await masterConnection.ExecuteCommandAsync(SqlBuilder.GetCreateTemplateCommand(DataFile, LogFile));

        FileExtensions.MarkFileAsWritable(DataFile);
        FileExtensions.MarkFileAsWritable(LogFile);

#if NET5_0_OR_GREATER
        await using (var connection = buildConnection(TemplateConnectionString))
#else
        using (var connection = buildConnection(TemplateConnectionString))
#endif
        {
            await connection.OpenAsync();
            await buildTemplate(connection);
            if (callback != null)
            {
                await callback(connection);
            }
        }

        await masterConnection.ExecuteCommandAsync(SqlBuilder.DetachTemplateCommand);

        File.SetCreationTime(DataFile, timestamp);
    }

    [Time]
    public void DeleteInstance(ShutdownMode mode = ShutdownMode.KillProcess)
    {
        LocalDbApi.StopAndDelete(instance, mode);
        System.IO.Directory.Delete(Directory, true);
        Dispose();
    }

    [Time]
    public void DeleteInstance(ShutdownMode mode, TimeSpan timeout)
    {
        LocalDbApi.StopAndDelete(instance, mode, timeout);
        System.IO.Directory.Delete(Directory, true);
        Dispose();
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

    public void Dispose() => semaphoreSlim.Dispose();
}

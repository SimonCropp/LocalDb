using MethodTimer;

class Wrapper : IDisposable
{
    public readonly string Directory;
    ushort size;
    Func<SqlConnection, Task>? callback;
    SemaphoreSlim semaphoreSlim = new(1, 1);
    public readonly string MasterConnectionString;
    string instance;
    public readonly string DataFile;
    public readonly string LogFile;
    public readonly string TemplateConnectionString;
    public readonly string ServerName;
    Task startupTask = null!;
    bool templateProvided;
    byte[]? dataFileBytes;
    byte[]? logFileBytes;

    public Wrapper(
        string instance,
        string directory,
        ushort size = 3,
        ExistingTemplate? existingTemplate = null,
        Func<SqlConnection, Task>? callback = null)
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

        await startupTask;

#if NET5_0_OR_GREATER
        await using var masterConnection = await OpenMasterConnection();
#else
        using var masterConnection = await OpenMasterConnection();
#endif
        await masterConnection.ExecuteCommandAsync(SqlBuilder.GetTakeDbsOfflineCommand(name));

        await FileExtensions.WriteFileAsync(dataFile, dataFileBytes!);
        await FileExtensions.WriteFileAsync(logFile, logFileBytes!);

        FileExtensions.MarkFileAsWritable(dataFile);
        FileExtensions.MarkFileAsWritable(logFile);

        var commandText = SqlBuilder.GetCreateOrMakeOnlineCommand(name, dataFile, logFile);
        await masterConnection.ExecuteCommandAsync(commandText);

        var connectionString = LocalDbSettings.BuildConnectionString(instance, name, false);
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

    public Task AwaitStart() => startupTask;

    void InnerStart(DateTime timestamp, Func<SqlConnection, Task> buildTemplate)
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
            await masterConnection.ExecuteCommandAsync(SqlBuilder.GetOptimizeModelDBCommand(size));
        }

        if (rebuildTemplate && !templateProvided)
        {
            await Rebuild(timestamp, buildTemplate, masterConnection);
        }

        dataFileBytes = await File.ReadAllBytesAsync(DataFile);
        logFileBytes = await File.ReadAllBytesAsync(LogFile);
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
        }

        await masterConnection.ExecuteCommandAsync(SqlBuilder.DetachTemplateCommand);

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

using System;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MethodTimer;
#if EF
using EfLocalDb;
#else
using LocalDb;
#endif

class Wrapper
{
    public readonly string Directory;
    ushort size;
    Func<DbConnection, Task>? callback;
    SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1,1);
    public readonly string MasterConnectionString;
    public readonly string WithRollbackConnectionString;
    Func<string, DbConnection> buildConnection;
    string instance;
    public readonly string DataFile;
    string LogFile;
    string TemplateConnectionString;
    public readonly string ServerName;
    Task startupTask = null!;
    DbConnection masterConnection = null!;
    bool templateProvided;

    public Wrapper(
        Func<string, DbConnection> buildConnection,
        string instance,
        string directory,
        ushort size = 3,
        ExistingTemplate? existingTemplate = null,
        Func<DbConnection, Task>? callback = null)
    {
        Guard.AgainstDatabaseSize(nameof(size), size);
        Guard.AgainstInvalidFileName(nameof(instance), instance);

        LocalDbLogging.WrapperCreated = true;
        this.buildConnection = buildConnection;
        this.instance = instance;
        MasterConnectionString = $"Data Source=(LocalDb)\\{instance};Database=master;MultipleActiveResultSets=True";
        TemplateConnectionString = $"Data Source=(LocalDb)\\{instance};Database=template;Pooling=false";
        WithRollbackConnectionString = $"Data Source=(LocalDb)\\{instance};Database=withRollback;Pooling=false";
        Directory = directory;

        LocalDbLogging.LogIfVerbose($"Directory: {directory}");
        this.size = size;
        this.callback = callback;
        if (existingTemplate == null)
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

    public async Task<string> CreateDatabaseFromTemplate(string name, bool withRollback = false)
    {
        if (string.Equals(name, "template", StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception("The database name 'template' is reserved.");
        }
        //TODO: if dataFile doesnt exists do a drop and recreate
        var stopwatch = Stopwatch.StartNew();

        // Explicitly dont take offline here, since that is done at startup
        var dataFile = Path.Combine(Directory, $"{name}.mdf");
        var logFile = Path.Combine(Directory, $"{name}_log.ldf");

        await startupTask;
        File.Copy(DataFile, dataFile, true);
        File.Copy(LogFile, logFile, true);

        FileExtensions.MarkFileAsWritable(dataFile);
        FileExtensions.MarkFileAsWritable(logFile);

        var commandText = SqlBuilder.GetCreateOrMakeOnlineCommand(name, dataFile, logFile, withRollback);
        await ExecuteOnMasterAsync(commandText);

        var connectionString = $"Data Source=(LocalDb)\\{instance};Database={name};MultipleActiveResultSets=True;Pooling=false";
        await RunCallback(connectionString);

        Trace.WriteLine($"Create DB `{name}` {stopwatch.ElapsedMilliseconds}ms.", "LocalDb");
        return connectionString;
    }

    async Task RunCallback(string connectionString)
    {
        if (callback == null)
        {
            return;
        }

        try
        {
            await semaphoreSlim.WaitAsync();
            if (callback == null)
            {
                return;
            }

            using var connection = buildConnection(connectionString);
            await connection.OpenAsync();
            await callback(connection);
            callback = null;
        }
        finally
        {
            semaphoreSlim.Release();
        }
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

    public Task AwaitStart()
    {
        return startupTask;
    }

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
            InitRollbackTask();
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

        InitRollbackTask();
    }

    [Time]
    async Task CreateAndDetachTemplate(
        DateTime timestamp,
        Func<DbConnection, Task> buildTemplate,
        bool rebuild,
        bool optimize)
    {
        masterConnection = buildConnection(MasterConnectionString);
        await masterConnection.OpenAsync();
        var takeDbsOffline = ExecuteOnMasterAsync(SqlBuilder.TakeDbsOfflineCommand);
        LocalDbLogging.LogIfVerbose($"SqlServerVersion: {masterConnection.ServerVersion}");

        if (optimize)
        {
            await ExecuteOnMasterAsync(SqlBuilder.GetOptimizationCommand(size));
        }

        if (rebuild && !templateProvided)
        {
            await Rebuild(timestamp, buildTemplate);
        }

        await takeDbsOffline;
    }

    async Task Rebuild(DateTime timestamp, Func<DbConnection, Task> buildTemplate)
    {
        DeleteTemplateFiles();
        await ExecuteOnMasterAsync(SqlBuilder.GetCreateTemplateCommand(DataFile, LogFile));

        FileExtensions.MarkFileAsWritable(DataFile);
        FileExtensions.MarkFileAsWritable(LogFile);

        using (var connection = buildConnection(TemplateConnectionString))
        {
            await connection.OpenAsync();
            await buildTemplate(connection);
        }

        await ExecuteOnMasterAsync(SqlBuilder.DetachTemplateCommand);

        File.SetCreationTime(DataFile, timestamp);
    }

    Task ExecuteOnMasterAsync(string command)
    {
        return masterConnection.ExecuteCommandAsync(command);
    }

    [Time]
    public void DeleteInstance()
    {
        LocalDbApi.StopAndDelete(instance);
        System.IO.Directory.Delete(Directory, true);
    }

    void DeleteTemplateFiles()
    {
        File.Delete(DataFile);
        File.Delete(LogFile);
    }

    [Time]
    public async Task DeleteDatabase(string dbName)
    {
        var commandText = SqlBuilder.BuildDeleteDbCommand(dbName);
        await ExecuteOnMasterAsync(commandText);
        var dataFile = Path.Combine(Directory, $"{dbName}.mdf");
        var logFile = Path.Combine(Directory, $"{dbName}_log.ldf");
        File.Delete(dataFile);
        File.Delete(logFile);
    }

    Lazy<Task> withRollbackTask = null!;

    void InitRollbackTask()
    {
        withRollbackTask = new Lazy<Task>(() => CreateDatabaseFromTemplate("withRollback", true));
    }

    public async Task CreateWithRollbackDatabase()
    {
        await startupTask;
        await withRollbackTask.Value;
    }
}
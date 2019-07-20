using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using MethodTimer;

class Wrapper
{
    string directory;
    ushort size;
    public readonly string MasterConnectionString;
    string instance;
    string TemplateDataFile;
    string TemplateLogFile;
    string TemplateConnectionString;
    public readonly string ServerName;
    Task startupTask;
    SqlConnection masterConnection;

    public Wrapper(string instance, string directory, ushort size = 3)
    {
        Guard.AgainstDatabaseSize(nameof(size), size);
        Guard.AgainstInvalidFileNameCharacters(nameof(instance), instance);

        LocalDbLogging.WrapperCreated = true;
        this.instance = instance;
        MasterConnectionString = $"Data Source=(LocalDb)\\{instance};Database=master;MultipleActiveResultSets=True";
        TemplateConnectionString = $"Data Source=(LocalDb)\\{instance};Database=template;Pooling=false";
        this.directory = directory;
        this.size = size;
        TemplateDataFile = Path.Combine(directory, "template.mdf");
        TemplateLogFile = Path.Combine(directory, "template_log.ldf");
        Directory.CreateDirectory(directory);
        ServerName = $@"(LocalDb)\{instance}";
    }

    public async Task<string> CreateDatabaseFromTemplate(string name)
    {
        //TODO: if dataFile doesnt exists do a drop and recreate
        var stopwatch = Stopwatch.StartNew();

        // Explicitly dont take offline here, since that is done at startup
        var dataFile = Path.Combine(directory, $"{name}.mdf");
        var logFile = Path.Combine(directory, $"{name}_log.ldf");
        var commandText = SqlCommandBuilder.GetCreateOrMakeOnlineCommand(name, dataFile, logFile);
        if (string.Equals(name, "template", StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception("The database name 'template' is reserved.");
        }

        await startupTask;
        File.Copy(TemplateDataFile, dataFile, true);
        File.Copy(TemplateLogFile, logFile, true);

        await ExecuteOnMasterAsync(commandText);
        Trace.WriteLine($"Create DB `{name}` {stopwatch.ElapsedMilliseconds}ms.", "LocalDb");
        return $"Data Source=(LocalDb)\\{instance};Database={name};MultipleActiveResultSets=True;Pooling=false";
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

        Trace.WriteLine(message, "LocalDb");
#if RELEASE
        }
        catch (Exception exception)
        {
            throw ExceptionBuilder.WrapLocalDbFailure(instance, directory, exception);
        }
#endif
    }

    public Task AwaitStart()
    {
        return startupTask;
    }

    void InnerStart(DateTime timestamp, Func<SqlConnection, Task> buildTemplate)
    {
        void CleanStart()
        {
            FileExtensions.FlushDirectory(directory);
            LocalDbApi.CreateInstance(instance);
            LocalDbApi.StartInstance(instance);
            startupTask = CreateAndDetachTemplate(
                timestamp,
                buildTemplate,
                rebuildTemplate: true,
                performOptimizations: true);
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

        var templateLastMod = File.GetCreationTime(TemplateDataFile);
        if (timestamp == templateLastMod)
        {
            LocalDbLogging.Log("Not modified so skipping rebuild");
            startupTask = CreateAndDetachTemplate(timestamp, buildTemplate, false, false);
        }
        else
        {
            startupTask = CreateAndDetachTemplate(timestamp, buildTemplate, true, false);
        }
    }

    [Time]
    async Task CreateAndDetachTemplate(DateTime timestamp, Func<SqlConnection, Task> buildTemplate, bool rebuildTemplate, bool performOptimizations)
    {
        masterConnection = new SqlConnection(MasterConnectionString);
        masterConnection.Open();
        var takeDbsOffline = ExecuteOnMasterAsync(SqlCommandBuilder.TakeDbsOfflineCommand);
        if (LocalDbLogging.Enabled)
        {
            Trace.WriteLine($"SqlServerVersion: {masterConnection.ServerVersion}", "LocalDb");
        }

        if (performOptimizations)
        {
            await ExecuteOnMasterAsync(SqlCommandBuilder.GetOptimizationCommand(size));
        }

        if (!rebuildTemplate)
        {
            await takeDbsOffline;
            return;
        }

        DeleteTemplateFiles();
        await ExecuteOnMasterAsync(SqlCommandBuilder.GetCreateTemplateCommand(TemplateDataFile, TemplateLogFile));

        using (var templateConnection = new SqlConnection(TemplateConnectionString))
        {
            await templateConnection.OpenAsync();
            await buildTemplate(templateConnection);
        }

        await ExecuteOnMasterAsync(SqlCommandBuilder.DetachTemplateCommand);

        File.SetCreationTime(TemplateDataFile, timestamp);
        await takeDbsOffline;
    }

    Task ExecuteOnMasterAsync(string command)
    {
        return masterConnection.ExecuteCommandAsync(command);
    }

    [Time]
    public void DeleteInstance()
    {
        LocalDbApi.StopAndDelete(instance);
        Directory.Delete(directory, true);
    }

    void DeleteTemplateFiles()
    {
        File.Delete(TemplateDataFile);
        File.Delete(TemplateLogFile);
    }

    [Time]
    public async Task DeleteDatabase(string dbName)
    {
        var commandText = SqlCommandBuilder.BuildDeleteDbCommand(dbName);
        await ExecuteOnMasterAsync(commandText);
        var dataFile = Path.Combine(directory, $"{dbName}.mdf");
        var logFile = Path.Combine(directory, $"{dbName}_log.ldf");
        File.Delete(dataFile);
        File.Delete(logFile);
    }

    public DatabaseState ReadDatabaseState(string dbName)
    {
        var dataFile = Path.Combine(directory, $"{dbName}.mdf");
        var logFile = Path.Combine(directory, $"{dbName}_log.ldf");
        var dbFileInfo = masterConnection.ReadFileInfo(dbName);

        return new DatabaseState
        {
            DataFileExists = File.Exists(dataFile),
            LogFileExists = File.Exists(logFile),
            DbDataFileName = dbFileInfo.data,
            DbLogFileName = dbFileInfo.log,
        };
    }
}
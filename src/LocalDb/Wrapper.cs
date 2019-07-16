using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using MethodTimer;
#if EF
using EfLocalDb;
#else
using LocalDb;
#endif

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
        if (size < 3)
        {
            throw new ArgumentOutOfRangeException(nameof(size), size, "3MB is the min allowed value");
        }

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

    static string GetDetachTemplateCommand()
    {
        return @"
if db_id('template') is not null
begin
  alter database [template] set single_user with rollback immediate;
  execute sp_detach_db 'template', 'true';
end;";
    }

    static string GetPurgeDbsCommand()
    {
        return @"
declare @command nvarchar(max)
set @command = ''

select @command = @command
+ '

EXEC sp_detach_db ''' + [name] + ''', ''true'';

'
from master.sys.databases
where [name] not in ('master', 'model', 'msdb', 'tempdb', 'template');
execute sp_executesql @command";
    }

    public async Task<string> CreateDatabaseFromTemplate(string name)
    {
        var stopwatch = Stopwatch.StartNew();

        var takeOfflineIfExistsText = $@"
if db_id('{name}') is not null
    alter database [{name}] set offline;
";
        var takeOfflineTask = ExecuteOnMasterAsync(takeOfflineIfExistsText);
        var dataFile = Path.Combine(directory, $"{name}.mdf");
        var logFile = Path.Combine(directory, $"{name}_log.ldf");
        var commandText = $@"
if db_id('{name}') is null
    begin
        create database [{name}] on
        (
            name = [{name}],
            filename = '{dataFile}'
        )
        for attach;

        alter database [{name}]
            modify file (name=template, newname='{name}')
        alter database [{name}]
            modify file (name=template_log, newname='{name}_log')
    end;
else
    begin
        alter database [{name}] set online;
    end;
";
        if (string.Equals(name, "template", StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception("The database name 'template' is reserved.");
        }

        await startupTask;
        await takeOfflineTask;
        File.Copy(TemplateDataFile, dataFile, true);
        File.Copy(TemplateLogFile, logFile, true);

        await ExecuteOnMasterAsync(commandText);
        Trace.WriteLine($"Create DB `{name}` {stopwatch.ElapsedMilliseconds}ms.", "LocalDb");
        return $"Data Source=(LocalDb)\\{instance};Database={name};MultipleActiveResultSets=True;Pooling=false";
    }

    string GetCreateTemplateCommand()
    {
        return $@"
if db_id('template') is not null
begin
  execute sp_detach_db 'template', 'true';
end;
create database template on
(
    name = template,
    filename = '{TemplateDataFile}',
    fileGrowth = 100KB
)
log on
(
    name = template_log,
    filename = '{TemplateLogFile}',
    size = 512KB,
    filegrowth = 100KB
);
";
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
        if (LocalDbLogging.Enabled)
        {
            Trace.WriteLine($"SqlServerVersion: {masterConnection.ServerVersion}");
        }

        if (performOptimizations)
        {
            await masterConnection.ExecuteCommandAsync(GetOptimizationCommand());
        }

        if (!rebuildTemplate)
        {
            return;
        }

        await masterConnection.ExecuteCommandAsync(GetPurgeDbsCommand());

        DeleteTemplateFiles();
        await masterConnection.ExecuteCommandAsync(GetCreateTemplateCommand());

        using (var templateConnection = new SqlConnection(TemplateConnectionString))
        {
            await templateConnection.OpenAsync();
            await buildTemplate(templateConnection);
        }

        await masterConnection.ExecuteCommandAsync(GetDetachTemplateCommand());

        File.SetCreationTime(TemplateDataFile, timestamp);
    }

    Task ExecuteOnMasterAsync(string command)
    {
        return masterConnection.ExecuteCommandAsync(command);
    }

    string GetOptimizationCommand()
    {
        return $@"
execute sp_configure 'show advanced options', 1;
reconfigure;
execute sp_configure 'user instance timeout', 30;
reconfigure;

-- begin-snippet: ShrinkModelDb
use model;
dbcc shrinkfile(modeldev, {size})
-- end-snippet
";
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
        var commandText = $@"
alter database [{dbName}] set single_user with rollback immediate;
drop database [{dbName}];";
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
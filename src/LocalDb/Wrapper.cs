using System;
using System.Data.SqlClient;
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
    string directory;
    ushort size;
    public readonly string MasterConnectionString;
    string instance;

    public Wrapper(string instance, string directory, ushort size)
    {
        if (size < 3)
        {
            throw new ArgumentOutOfRangeException(nameof(size), size, "3MB is the min allowed value");
        }
        Guard.AgainstInvalidFileNameCharacters(nameof(instance),instance);

        this.instance = instance;
        MasterConnectionString = $"Data Source=(LocalDb)\\{instance};Database=master";
        TemplateConnection = $"Data Source=(LocalDb)\\{instance};Database=template;Pooling=false";
        this.directory = directory;
        this.size = size;
        TemplateDataFile = Path.Combine(directory, "template.mdf");
        TemplateLogFile = Path.Combine(directory, "template_log.ldf");
        Directory.CreateDirectory(directory);
        ServerName = $@"(LocalDb)\{instance}";
    }

    string TemplateDataFile;
    string TemplateLogFile;
    string TemplateConnection;
    public readonly string ServerName;
    Task<Guid> createDatabaseTask;

    [Time]
    void DetachTemplate(DateTime timestamp)
    {
        var commandText = @"
if db_id('template') is not null
begin
  alter database [template] set single_user with rollback immediate;
  execute sp_detach_db 'template', 'true';
end;";

        ExecuteOnMaster(commandText);
        File.SetCreationTime(TemplateDataFile, timestamp);
    }

    [Time]
    void PurgeDbs()
    {
        var commandText = @"
declare @command nvarchar(max)
set @command = ''

select @command = @command
+ '

EXEC sp_detach_db ''' + [name] + ''', ''true'';

'
from master.sys.databases
where [name] not in ('master', 'model', 'msdb', 'tempdb', 'template');
execute sp_executesql @command";
        ExecuteOnMaster(commandText);
    }

    async Task<Guid> CreateDatabaseTask()
    {
        var name = Guid.NewGuid();
        var dataFile = Path.Combine(directory, $"{name}.mdf");
        File.Copy(TemplateDataFile, dataFile);
        var commandText = $@"
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
";
        await ExecuteOnMasterAsync(commandText);
        return name;
    }

    [Time]
    public async Task<(string connection, Guid id)> CreateDatabaseFromTemplate(string name)
    {
        var stopwatch = Stopwatch.StartNew();
        if (string.Equals(name, "template", StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception("The database name 'template' is reserved.");
        }

        var newTask = CreateDatabaseTask();
        var result = Interlocked.Exchange(ref createDatabaseTask,newTask);

        var guid = await result;

        var commandText = $"alter database [{guid}] modify name = [{name}];";
        await ExecuteOnMasterAsync(commandText);
        Trace.WriteLine($"Create DB `{name}` {stopwatch.ElapsedMilliseconds}ms.", "LocalDb");
        return ($"Data Source=(LocalDb)\\{instance};Database={name};MultipleActiveResultSets=True;Pooling=false",guid);
    }

    [Time]
    void CreateTemplate()
    {
        var commandText = $@"
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
        ExecuteOnMaster(commandText);
    }

    [Time]
    public void Start(DateTime timestamp, Action<SqlConnection> buildTemplate)
    {
#if RELEASE
        try
        {
#endif
            var stopwatch = Stopwatch.StartNew();
            InnerStart(timestamp, buildTemplate);
            var message = $"Start `{ServerName}` {stopwatch.ElapsedMilliseconds}ms.";
            if (LocalDbLogging.Enabled)
            {
                using (var connection = new SqlConnection(MasterConnectionString))
                {
                    connection.Open();
                    message += $"{Environment.NewLine} ServerVersion: {connection.ServerVersion}";
                }
            }
            Trace.WriteLine(message, "LocalDb");
#if RELEASE
        }
        catch (Exception exception)
        {
            throw ExceptionBuilder.WrapLocalDbFailure(instance, directory, exception);
        }
#endif
    }

    void InnerStart(DateTime timestamp, Action<SqlConnection> buildTemplate)
    {
        void CleanStart()
        {
            FileExtensions.FlushDirectory(directory);
            LocalDbApi.CreateInstance(instance);
            LocalDbApi.StartInstance(instance);
            RunOnceOffOptimizations();
            CreateTemplate();
            ExecuteBuildTemplate(buildTemplate);
            DetachTemplate(timestamp);
            createDatabaseTask = CreateDatabaseTask();
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

        PurgeDbs();
        DeleteNonTemplateFiles();

        var templateLastMod = File.GetCreationTime(TemplateDataFile);
        if (timestamp == templateLastMod)
        {
            LocalDbLogging.Log("Not modified so skipping rebuild");
            createDatabaseTask = CreateDatabaseTask();
            return;
        }

        DeleteTemplateFiles();
        CreateTemplate();
        ExecuteBuildTemplate(buildTemplate);
        DetachTemplate(timestamp);
        createDatabaseTask = CreateDatabaseTask();
    }

    [Time]
    void ExecuteBuildTemplate(Action<SqlConnection> buildTemplate)
    {
        using (var connection = new SqlConnection(TemplateConnection))
        {
            connection.Open();
            buildTemplate(connection);
        }
    }

    async Task ExecuteOnMasterAsync(string command)
    {
        using (var connection = new SqlConnection(MasterConnectionString))
        {
            await connection.OpenAsync();
            await connection.ExecuteCommandAsync(command);
        }
    }

    void ExecuteOnMaster(string command)
    {
        using (var connection = new SqlConnection(MasterConnectionString))
        {
            connection.Open();
            connection.ExecuteCommand(command);
        }
    }

    [Time]
    void RunOnceOffOptimizations()
    {
        var commandText = $@"
execute sp_configure 'show advanced options', 1;
reconfigure;
execute sp_configure 'user instance timeout', 30;
reconfigure;

-- begin-snippet: ShrinkModelDb
use model;
dbcc shrinkfile(modeldev, {size})
-- end-snippet
";
        ExecuteOnMaster(commandText);
    }

    [Time]
    public async Task DeleteInstance()
    {
        await createDatabaseTask;
        LocalDbApi.StopAndDelete(instance);
        Directory.Delete(directory, true);
    }

    [Time]
    void DeleteNonTemplateFiles()
    {
        foreach (var file in Directory.EnumerateFiles(directory))
        {
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(file);
            if (nameWithoutExtension == "template")
            {
                continue;
            }
            if (nameWithoutExtension == "template_log")
            {
                continue;
            }

            File.Delete(file);
        }
    }

    void DeleteTemplateFiles()
    {
        File.Delete(TemplateDataFile);
        File.Delete(TemplateLogFile);
    }

    [Time]
    public async Task DeleteDatabase(string dbName)
    {
        var commandText = $"drop database [{dbName}];";
        await ExecuteOnMasterAsync(commandText);
        var dataFile = Path.Combine(directory, $"{dbName}.mdf");
        var logFile = Path.Combine(directory, $"{dbName}_log.ldf");
        File.Delete(dataFile);
        File.Delete(logFile);
    }
}
using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

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
        TemplateConnection = $"Data Source=(LocalDb)\\{instance};Database=template";
        this.directory = directory;
        this.size = size;
        TemplateDataFile = Path.Combine(directory, "template.mdf");
        TemplateLogFile = Path.Combine(directory, "template_log.ldf");
        Directory.CreateDirectory(directory);
        ServerName = $@"(LocalDb)\{instance}";
        Trace.WriteLine($"Creating LocalDb instance. Server Name: {ServerName}");
    }

    string TemplateDataFile;
    string TemplateLogFile;
    string TemplateConnection;
    public readonly string ServerName;

    void DetachTemplate()
    {
        var commandText = @"
if db_id('template') is not null
begin
  alter database [template] set single_user with rollback immediate;
  exec sp_detach_db 'template', 'true';
end;";

        ExecuteOnMaster(commandText);
    }

    void BringTemplateOnline()
    {
        var commandText = @"alter database [template] set online";
        ExecuteOnMaster(commandText);
    }

    void TakeTemplateOffline(DateTime? timestamp)
    {
        var commandText = @"
alter database [template]
set offline
with rollback immediate";
        ExecuteOnMaster(commandText);
        if (timestamp != null)
        {
            File.SetCreationTime(TemplateDataFile, timestamp.Value);
        }
    }

    void PurgeDbs()
    {
        var commandText = @"
declare @command nvarchar(max)
set @command = ''

select @command = @command
+ '

begin try
  alter database [' + [name] + '] set single_user with rollback immediate;
end try
begin catch
end catch;

drop database [' + [name] + '];

'
from master.sys.databases
where [name] not in ('master', 'model', 'msdb', 'tempdb', 'template');
execute sp_executesql @command";
        ExecuteOnMaster(commandText);
    }

    public Task<string> CreateDatabaseFromTemplate(string name)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            return InnerCreateDatabaseFromTemplate(name);
        }
        finally
        {
            Trace.WriteLine($"LocalDB CreateDatabaseFromTemplate: {stopwatch.ElapsedMilliseconds}ms");
        }
    }

    Task<string> InnerCreateDatabaseFromTemplate(string name)
    {
        if (string.Equals(name, "template", StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception("The database name 'template' is reserved.");
        }

        var dataFile = Path.Combine(directory, $"{name}.mdf");
        if (File.Exists(dataFile))
        {
            throw new Exception($"The database name '{name}' has already been used.");
        }

        var templateDataFile = Path.Combine(directory, "template.mdf");

        var copyTask = FileExtensions.Copy(templateDataFile, dataFile);

        return CreateDatabaseFromFile(name, copyTask);
    }

    void RestoreTemplate()
    {
        var dataFile = Path.Combine(directory, "template.mdf");
        var commandText = $@"
if db_id('template') is null
begin
    create database template on
    (
        name = template,
        filename = '{dataFile}'
    )
    for attach;
    alter database [template] set offline;
end;
";
        ExecuteOnMaster(commandText);
    }

    async Task<string> CreateDatabaseFromFile(string name, Task fileCopyTask)
    {
        var dataFile = Path.Combine(directory, $"{name}.mdf");
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
        using (var connection = new SqlConnection(MasterConnectionString))
        {
            await connection.OpenAsync();
            await fileCopyTask;
            await connection.ExecuteCommandAsync(commandText);
        }

        return $"Data Source=(LocalDb)\\{instance};Database={name};MultipleActiveResultSets=True";
    }

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

    public void Start(Func<SqlConnection, bool> requiresRebuild, DateTime? timestamp, Action<SqlConnection> buildTemplate)
    {
        void CleanStart()
        {
            FileExtensions.FlushDirectory(directory);
            LocalDbApi.CreateInstance(instance);
            LocalDbApi.StartInstance(instance);
            ShrinkModelDb();
            CreateTemplate();
            ExecuteBuildTemplate(buildTemplate);
            TakeTemplateOffline(timestamp);
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

        //TODO: remove in future. only required for old versions that used detach
        RestoreTemplate();
        if (requiresRebuild == null)
        {
            if (timestamp != null)
            {
                var templateLastMod = File.GetCreationTime(TemplateDataFile);
                if (timestamp == templateLastMod)
                {
                    Trace.WriteLine("Not modified so skipping rebuild");
                    return;
                }
            }
        }
        else
        {
            BringTemplateOnline();
            if (!ExecuteRequiresRebuild(requiresRebuild))
            {
                TakeTemplateOffline(timestamp);
                return;
            }
        }

        DetachTemplate();
        DeleteTemplateFiles();
        CreateTemplate();
        ExecuteBuildTemplate(buildTemplate);
        TakeTemplateOffline(timestamp);
    }

    bool ExecuteRequiresRebuild(Func<SqlConnection, bool> requiresRebuild)
    {
        using (var connection = new SqlConnection(TemplateConnection))
        {
            connection.Open();
            return requiresRebuild(connection);
        }
    }

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

    void ShrinkModelDb()
    {
        var commandText = $@"
-- begin-snippet: ShrinkModelDb
use model;
dbcc shrinkfile(modeldev, {size})
-- end-snippet
";
        ExecuteOnMaster(commandText);
    }

    public void DeleteInstance()
    {
        LocalDbApi.StopAndDelete(instance);
        Directory.Delete(directory, true);
    }

    void DeleteNonTemplateFiles()
    {
        foreach (var file in Directory.EnumerateFiles(directory))
        {
            if (Path.GetFileNameWithoutExtension(file) == "template")
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

    public async Task DeleteDatabase(string dbName)
    {
        var commandText = $@"
alter database [{dbName}] set single_user with rollback immediate;
drop database [{dbName}];";
        await ExecuteOnMasterAsync(commandText);
        var dataFile = Path.Combine(directory, $"{dbName}.mdf");
        var logFile = Path.Combine(directory, $"{dbName}.ldf");
        File.Delete(dataFile);
        File.Delete(logFile);
    }
}
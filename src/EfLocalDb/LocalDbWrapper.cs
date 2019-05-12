using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

class LocalDbWrapper
{
    string directory;
    string masterConnection;
    string instance;

    public LocalDbWrapper(string instance, string directory)
    {
        this.instance = instance;
        masterConnection = $"Data Source=(LocalDb)\\{instance};Database=master; Integrated Security=True";
        this.directory = directory;
        Directory.CreateDirectory(directory);
    }

    public void Detach(string name)
    {
        using (var connection = new SqlConnection(masterConnection))
        using (var command = connection.CreateCommand())
        {
            connection.Open();
            command.CommandText = $"EXEC sp_detach_db '{name}', 'true';";
            command.ExecuteNonQuery();
        }
    }

    public void Purge()
    {
        using (var connection = new SqlConnection(masterConnection))
        using (var command = connection.CreateCommand())
        {
            connection.Open();
            command.CommandText = @"
DECLARE @command nvarchar(max)
SET @command = ''

SELECT  @command = @command
+ 'ALTER DATABASE [' + [name] + ']  SET single_user with rollback immediate;'+CHAR(13)+CHAR(10)
+ 'DROP DATABASE [' + [name] +'];'+CHAR(13)+CHAR(10)
FROM  [master].[sys].[databases] 
 where [name] not in ( 'master', 'model', 'msdb', 'tempdb');

SELECT @command
EXECUTE sp_executesql @command";
            command.ExecuteNonQuery();
        }
    }

    public async Task<string> CreateDatabaseFromTemplate(string name, string templateName)
    {
        var dataFile = Path.Combine(directory, $"{name}.mdf");
        var templateDataFile = Path.Combine(directory, templateName + ".mdf");

        File.Copy(templateDataFile, dataFile);

        using (var connection = new SqlConnection(masterConnection))
        using (var command = connection.CreateCommand())
        {
            command.CommandText = $@"
create database [{name}] on
(
    name = [{name}],
    filename = '{dataFile}',
    size = 10MB,
    maxSize = 10GB,
    fileGrowth = 5MB
)
for attach;
";
            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        return $"Data Source=(LocalDb)\\{instance};Database={name}; Integrated Security=True";
    }

    public string CreateDatabase(string name)
    {
        var dataFile = Path.Combine(directory, name + ".mdf");
        using (var connection = new SqlConnection(masterConnection))
        using (var command = connection.CreateCommand())
        {
            command.CommandText = $@"
create database [{name}] on
(
    name = [{name}],
    filename = '{dataFile}',
    size = 10MB,
    maxSize = 10GB,
    fileGrowth = 5MB
);
";
            connection.Open();
            command.ExecuteNonQuery();
        }
        return $"Data Source=(LocalDb)\\{instance};Database=template; Integrated Security=True";
    }

    public void Start()
    {
        RunLocalDbCommand($"create \"{instance}\"");
        RunLocalDbCommand($"start \"{instance}\"");
    }

    public void DeleteInstance()
    {
        RunLocalDbCommand($"stop \"{instance}\"");
        RunLocalDbCommand($"delete \"{instance}\"");
        DeleteFiles();
    }

    public void DeleteFiles()
    {
        foreach (var file in Directory.EnumerateFiles(directory))
        {
            File.Delete(file);
        }
    }

    static void RunLocalDbCommand(string command)
    {
        using (var start = Process.Start("sqllocaldb", command))
        {
            start.WaitForExit();
        }
    }
}
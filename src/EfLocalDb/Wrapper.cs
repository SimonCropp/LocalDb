using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

class Wrapper
{
    string directory;
    string masterConnection;
    string instance;

    public Wrapper(string instance, string directory)
    {
        this.instance = instance;
        masterConnection = $"Data Source=(LocalDb)\\{instance};Database=master; Integrated Security=True";
        this.directory = directory;
        Directory.CreateDirectory(directory);
    }

    public void Detach(string name)
    {
        var commandText = $"EXEC sp_detach_db '{name}', 'true';";
        try
        {
            using (var connection = new SqlConnection(masterConnection))
            using (var command = connection.CreateCommand())
            {
                connection.Open();
                command.CommandText = commandText;
                command.ExecuteNonQuery();
            }
        }
        catch (Exception exception)
        {
            throw new Exception(
                innerException: exception,
                message: $@"Failed to {nameof(Detach)}
{nameof(directory)}: {directory}
{nameof(masterConnection)}: {masterConnection}
{nameof(instance)}: {instance}
{nameof(commandText)}: {commandText}
");
        }
    }

    public void Purge()
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
from [master].[sys].[databases]
where [name] not in ('master', 'model', 'msdb', 'tempdb');
execute sp_executesql @command";
            try
            {
                using (var connection = new SqlConnection(masterConnection))
                using (var command = connection.CreateCommand())
                {
                    connection.Open();
                    command.CommandText = commandText;
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception exception)
            {
                throw new Exception(
                    innerException: exception,
                    message: $@"Failed to {nameof(Purge)}
{nameof(directory)}: {directory}
{nameof(masterConnection)}: {masterConnection}
{nameof(instance)}: {instance}
{nameof(commandText)}: {commandText}
");
            }
    }

    public async Task<string> CreateDatabaseFromTemplate(string name, string templateName)
    {
        var dataFile = Path.Combine(directory, $"{name}.mdf");
        var templateDataFile = Path.Combine(directory, templateName + ".mdf");

        File.Copy(templateDataFile, dataFile);
        var commandText = $@"
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
        try
        {
            using (var connection = new SqlConnection(masterConnection))
            using (var command = connection.CreateCommand())
            {
                command.CommandText = commandText;
                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();
            }
        }
        catch (Exception exception)
        {
            throw new Exception(
                innerException: exception,
                message: $@"Failed to {nameof(CreateDatabaseFromTemplate)}
{nameof(directory)}: {directory}
{nameof(masterConnection)}: {masterConnection}
{nameof(instance)}: {instance}
{nameof(name)}: {name}
{nameof(templateName)}: {templateName}
{nameof(dataFile)}: {dataFile}
{nameof(templateDataFile)}: {templateDataFile}
{nameof(commandText)}: {commandText}
");
        }

        return $"Data Source=(LocalDb)\\{instance};Database={name}; Integrated Security=True";
    }

    public string CreateDatabase(string name)
    {
        var dataFile = Path.Combine(directory, name + ".mdf");
        var commandText = $@"
create database [{name}] on
(
    name = [{name}],
    filename = '{dataFile}',
    size = 10MB,
    maxSize = 10GB,
    fileGrowth = 5MB
);
";
        try
        {
            using (var connection = new SqlConnection(masterConnection))
            using (var command = connection.CreateCommand())
            {
                command.CommandText = commandText;
                connection.Open();
                command.ExecuteNonQuery();
            }
        }
        catch (Exception exception)
        {
            throw new Exception(
                innerException: exception,
                message: $@"Failed to {nameof(CreateDatabase)}
{nameof(directory)}: {directory}
{nameof(masterConnection)}: {masterConnection}
{nameof(instance)}: {instance}
{nameof(name)}: {name}
{nameof(dataFile)}: {dataFile}
{nameof(commandText)}: {commandText}
");
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

    void RunLocalDbCommand(string command)
    {
        try
        {
            using (var start = Process.Start("sqllocaldb", command))
            {
                start.WaitForExit();
            }
        }
        catch (Exception exception)
        {
            throw new Exception(
                innerException: exception,
                message: $@"Failed to {nameof(RunLocalDbCommand)}
{nameof(directory)}: {directory}
{nameof(masterConnection)}: {masterConnection}
{nameof(instance)}: {instance}
{nameof(command)}: sqllocaldb {command}
");
        }
    }
}
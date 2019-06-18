﻿using System;
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
        masterConnection = $"Data Source=(LocalDb)\\{instance};Database=master";
        this.directory = directory;
        Directory.CreateDirectory(directory);
        ServerName = $@"(LocalDb)\{instance}";
        Trace.WriteLine($@"Creating LocalDb instance. Server Name: {ServerName}");
    }

    public readonly string ServerName;

    public void DetachTemplate()
    {
        var commandText = @"exec sp_detach_db 'template', 'true';";
        try
        {
            using (var connection = new SqlConnection(masterConnection))
            {
                connection.Open();
                connection.ExecuteCommand(commandText);
            }
        }
        catch (Exception exception)
        {
            throw new Exception(
                innerException: exception,
                message: $@"Failed to {nameof(DetachTemplate)}
{nameof(directory)}: {directory}
{nameof(instance)}: {instance}
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
from master.sys.databases
where [name] not in ('master', 'model', 'msdb', 'tempdb');
execute sp_executesql @command";
        try
        {
            using (var connection = new SqlConnection(masterConnection))
            {
                connection.Open();
                connection.ExecuteCommand(commandText);
            }
        }
        catch (Exception exception)
        {
            throw new Exception(
                innerException: exception,
                message: $@"Failed to {nameof(Purge)}
{nameof(directory)}: {directory}
{nameof(instance)}: {instance}
");
        }
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

    public bool TemplateFileExists()
    {
        var dataFile = Path.Combine(directory, "template.mdf");
        return File.Exists(dataFile);
    }

    public string RestoreTemplate()
    {
        var dataFile = Path.Combine(directory, "template.mdf");
        var commandText = $@"
create database template on
(
    name = template,
    filename = '{dataFile}'
)
for attach;
";
        try
        {
            using (var connection = new SqlConnection(masterConnection))
            {
                connection.Open();
                connection.ExecuteCommand(commandText);
            }
        }
        catch (Exception exception)
        {
            throw BuildException("template", exception, nameof(RestoreTemplate), dataFile);
        }

        // needs to be pooling=false so that we can immediately detach and use the files
        return $"Data Source=(LocalDb)\\{instance};Database=template;MultipleActiveResultSets=True;Pooling=false";
    }

    public async Task<string> CreateDatabaseFromFile(string name, Task fileCopyTask)
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
        try
        {
            using (var connection = new SqlConnection(masterConnection))
            {
                await connection.OpenAsync();
                await fileCopyTask;
                await connection.ExecuteCommandAsync(commandText);
            }
        }
        catch (Exception exception)
        {
            throw BuildException(name, exception, nameof(CreateDatabaseFromFile), dataFile);
        }

        return $"Data Source=(LocalDb)\\{instance};Database={name};MultipleActiveResultSets=True";
    }

    public string CreateTemplate()
    {
        var dataFile = Path.Combine(directory, "template.mdf");
        var logFile = Path.Combine(directory, "template_log.ldf");
        try
        {
            using (var connection = new SqlConnection(masterConnection))
            {
                connection.Open();
                var commandText = $@"
create database template on
(
    name = template,
    filename = '{dataFile}',
    size = 3MB,
    fileGrowth = 100KB
)
log on
(
    name = template_log,
    filename = '{logFile}',
    size = 512KB,
    filegrowth = 100KB );
";
                connection.ExecuteCommand(commandText);
            }
        }
        catch (Exception exception)
        {
            throw new Exception(
                innerException: exception,
                message: $@"Failed to {nameof(CreateTemplate)}
{nameof(directory)}: {directory}
{nameof(instance)}: {instance}
{nameof(dataFile)}: {dataFile}
");
        }

        return $"Data Source=(LocalDb)\\{instance};Database=template;MultipleActiveResultSets=True;Pooling=false";
    }

    public void Start()
    {
        if (SqlLocalDb.Start(instance) == State.NotExists)
        {
            var commandText = @"
use model;
dbcc shrinkfile(modeldev, 3)";
            using (var connection = new SqlConnection(masterConnection))
            {
                connection.Open();
                connection.ExecuteCommand(commandText);
            }
        }
    }

    public void DeleteInstance()
    {
        SqlLocalDb.DeleteInstance(instance);
        DeleteFiles();
    }

    public void DeleteFiles(string exclude = null)
    {
        foreach (var file in Directory.EnumerateFiles(directory))
        {
            if (exclude != null)
            {
                if (Path.GetFileNameWithoutExtension(file) == exclude)
                {
                    continue;
                }
            }

            File.Delete(file);
        }
    }

    Exception BuildException(string name, Exception exception, string methodName, string dataFile)
    {
        return new Exception(
            innerException: exception,
            message: $@"Failed to {methodName}
{nameof(directory)}: {directory}
{nameof(instance)}: {instance}
{nameof(name)}: {name}
{nameof(dataFile)}: {dataFile}
");
    }
}
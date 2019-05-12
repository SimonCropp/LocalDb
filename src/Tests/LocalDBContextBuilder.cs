using System;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

public static class LocalDBContextBuilder
{
    static string dataDirectory;
    static string templateDataFile;
    static string templateLogFile;
    static string masterConnection;
    static string key;

    public static void Init(string key)
    {
        LocalDBContextBuilder.key = key;
        masterConnection = $"Data Source=(LocalDb)\\{key};Database=master; Integrated Security=True";
        dataDirectory = Environment.GetEnvironmentVariable("AGENT_TEMPDIRECTORY");
        dataDirectory = dataDirectory ?? Environment.GetEnvironmentVariable("LocalDBData");
        dataDirectory = dataDirectory ?? Path.GetTempPath();
        dataDirectory = Path.Combine(dataDirectory, key);
        Directory.CreateDirectory(dataDirectory);

        templateDataFile = Path.Combine(dataDirectory, "template.mdf");
        templateLogFile = Path.Combine(dataDirectory, "template.ldf");
        LocalDbCommands.ResetLocalDb(key,dataDirectory);

        RecreateTemplateDatabase();
        // needs to be pooling=false so that we can immediately detach and use the files
        var connectionString = $"Data Source=(LocalDb)\\{key};Database=template; Integrated Security=True;Pooling=false";
        using (var connection = new SqlConnection(connectionString))
        {
            connection.Open();

            Migrate(connection);
        }

        Detach( "template");
    }

    static void Detach(string dbName)
    {
        using (var connection = new SqlConnection(masterConnection))
        using (var command = connection.CreateCommand())
        {
            connection.Open();
            command.CommandText = $"EXEC sp_detach_db '{dbName}', 'true';";
            command.ExecuteNonQuery();
        }
    }

    public static async Task<string> BuildContext(string dbName)
    {
        var dbFilePath = Path.Combine(dataDirectory, $"{dbName}.mdf");
        var dbLogPath = Path.Combine(dataDirectory, $"{dbName}.ldf");

        File.Copy(templateDataFile, dbFilePath);
        File.Copy(templateLogFile, dbLogPath);

        using (var connection = new SqlConnection(masterConnection))
        using (var command = connection.CreateCommand())
        {
            command.CommandText = $@"
create database [{dbName}] on
(
    name = [{dbName}],
    filename = '{dbFilePath}',
    size = 10MB,
    maxSize = 10GB,
    fileGrowth = 5MB
)
log on
(
    name = [{dbName}_log],
    filename = '{dbLogPath}',
    size = 10MB,
    maxSize = 10GB,
    fileGrowth = 5MB
)
for attach;
";
            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        return $"Data Source=(LocalDb)\\{key};Database={dbName}; Integrated Security=True";
    }

    static void Migrate(SqlConnection connection)
    {
        var builder = new DbContextOptionsBuilder<TestDataContext>();
        builder.ConfigureWarnings(warnings => warnings.Throw(CoreEventId.IncludeIgnoredWarning));
        builder.UseSqlServer(connection);
        //TODO:
        //optionsBuilder.ReplaceService<IMigrationsSqlGenerator, CustomMigrationsSqlGenerator>();
        using (var dataContext = new TestDataContext(builder.Options))
        {
            dataContext.Database.EnsureCreated();
            //TODO:
            //dataContext.Database.Migrate();
        }
       //TODO:
       // TrackChanges.EnableChangeTrackingOnDb(connection);
    }

    static void RecreateTemplateDatabase()
    {
        using (var connection = new SqlConnection(masterConnection))
        using (var command = connection.CreateCommand())
        {
            command.CommandText = $@"
create database [template] on
(
    name = [template],
    filename = '{templateDataFile}',
    size = 10MB,
    maxSize = 10GB,
    fileGrowth = 5MB
)
log on
(
    name = [template_log],
    filename = '{templateLogFile}',
    size = 10MB,
    maxSize = 10GB,
    fileGrowth = 5MB
);
";
            connection.Open();
            command.ExecuteNonQuery();
        }
    }
}
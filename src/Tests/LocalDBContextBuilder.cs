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
    static string key;
    
    static LocalDbWrapper localDbWrapper;

    public static void Init(string key)
    {
        LocalDBContextBuilder.key = key;

        dataDirectory = Environment.GetEnvironmentVariable("AGENT_TEMPDIRECTORY");
        dataDirectory = dataDirectory ?? Environment.GetEnvironmentVariable("LocalDBData");
        dataDirectory = dataDirectory ?? Path.GetTempPath();
        dataDirectory = Path.Combine(dataDirectory, key);
        localDbWrapper = new LocalDbWrapper(key, dataDirectory);

        templateDataFile = Path.Combine(dataDirectory, "template.mdf");
        templateLogFile = Path.Combine(dataDirectory, "template.ldf");
        LocalDbCommands.ResetLocalDb(key,dataDirectory);

        localDbWrapper.CreateDatabase("template");
        // needs to be pooling=false so that we can immediately detach and use the files
        var connectionString = $"Data Source=(LocalDb)\\{key};Database=template; Integrated Security=True;Pooling=false";
        using (var connection = new SqlConnection(connectionString))
        {
            connection.Open();

            Migrate(connection);
        }

        localDbWrapper.Detach( "template");
    }

    public static async Task<string> BuildContext(string dbName)
    {
        var dbFilePath = Path.Combine(dataDirectory, $"{dbName}.mdf");
        var dbLogPath = Path.Combine(dataDirectory, $"{dbName}.ldf");

        File.Copy(templateDataFile, dbFilePath);
        File.Copy(templateLogFile, dbLogPath);

        using (var connection = new SqlConnection(localDbWrapper.MasterConnection))
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
}
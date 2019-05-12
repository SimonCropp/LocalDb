using System;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

public static class LocalDBContextBuilder
{
    static LocalDbWrapper localDbWrapper;

    public static void Init(string key)
    {
        var dataDirectory = Environment.GetEnvironmentVariable("AGENT_TEMPDIRECTORY");
        dataDirectory = dataDirectory ?? Environment.GetEnvironmentVariable("LocalDBData");
        dataDirectory = dataDirectory ?? Path.GetTempPath();
        dataDirectory = Path.Combine(dataDirectory, key);
        localDbWrapper = new LocalDbWrapper(key, dataDirectory);

        localDbWrapper.ResetLocalDb();

        var connectionString = localDbWrapper.CreateDatabase("template");
        // needs to be pooling=false so that we can immediately detach and use the files
        connectionString+= ";Pooling=false";
        using (var connection = new SqlConnection(connectionString))
        {
            connection.Open();

            Migrate(connection);
        }

        localDbWrapper.Detach("template");
    }

    public static Task<string> BuildContext(string dbName)
    {
       return localDbWrapper.CreateDatabaseFromTemplate(dbName, "template");
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
using System;
using System.Data.SqlClient;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

public class LocalDB
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



    public string ConnectionString;

    /// <summary>
    ///   Build DB with a name based on the calling Method
    /// </summary>
    /// <param name="caller">Normally pass this </param>
    /// <param name="suffix">For Xunit theories add some text based on the inline data to make the db name unique</param>
    /// <param name="memberName">do not use, will default to the caller method name is used</param>
    public static async Task<LocalDB> Build(object caller, string suffix = null, [CallerMemberName] string memberName = null)
    {
        var type = caller.GetType();
        var dbName = $"{type.Name}_{memberName}";
        if (suffix != null)
        {
            dbName = $"{dbName}_{suffix}";
        }

        return new LocalDB
        {
            ConnectionString = await BuildContext(dbName)
        };
    }

    public async Task AddSeed(params object[] entities)
    {
        using (var seedingDataContext = NewDataContext())
        {
            seedingDataContext.AddRange(entities);
            await seedingDataContext.SaveChangesAsync();
        }
    }

    public TestDataContext NewDataContext()
    {
        var builder = new DbContextOptionsBuilder<TestDataContext>();
        builder.UseSqlServer(ConnectionString);
        return new TestDataContext(builder.Options);
    }
}
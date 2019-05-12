using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public class LocalDB
{
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
            ConnectionString = await LocalDBContextBuilder.BuildContext(dbName)
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
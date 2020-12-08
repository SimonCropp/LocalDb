#if(!NETCOREAPP3_1)
using System.Threading.Tasks;
using EfLocalDb;
using Xunit;

public class EfSnippetTests
{
    static SqlInstance<MyDbContext> sqlInstance;

    static EfSnippetTests()
    {
        sqlInstance = new(
            connection => new(connection));
    }

    [Fact]
    public async Task TheTest()
    {
        #region EfClassicBuildDatabase
        using var database = await sqlInstance.Build();
        #endregion
        #region EfClassicBuildContext
        using (var data = database.NewDbContext())
        {
            #endregion
            TheEntity entity = new()
            {
                Property = "prop"
            };
            data.TestEntities.Add(entity);
            await data.SaveChangesAsync();
        }

        using (var data = database.NewDbContext())
        {
            Assert.Single(data.TestEntities);
        }
    }

    [Fact]
    public async Task TheTestWithDbName()
    {
        using var database = await sqlInstance.Build("TheTestWithDbName");
        TheEntity entity = new()
        {
            Property = "prop"
        };
        await database.AddData(entity);

        Assert.Single(database.Context.TestEntities);
    }
}
#endif
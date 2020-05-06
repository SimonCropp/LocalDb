using System.Threading.Tasks;
using EfLocalDb;
using Xunit;

public class EfSnippetTests
{
    static SqlInstance<MyDbContext> sqlInstance;

    static EfSnippetTests()
    {
        sqlInstance = new SqlInstance<MyDbContext>(
            connection => new MyDbContext(connection));
    }

    [Fact]
    public async Task TheTest()
    {
        #region EfClassicBuildDatabase
        using var database = await sqlInstance.Build();
        #endregion
        #region EfClassicBuildContext
        using (var dbContext = database.NewDbContext())
        {
            #endregion
            var entity = new TheEntity
            {
                Property = "prop"
            };
            dbContext.TestEntities.Add(entity);
            await dbContext.SaveChangesAsync();
        }

        using (var dbContext = database.NewDbContext())
        {
            Assert.Single(dbContext.TestEntities);
        }
    }

    [Fact]
    public async Task TheTestWithDbName()
    {
        using var database = await sqlInstance.Build("TheTestWithDbName");
        var entity = new TheEntity
        {
            Property = "prop"
        };
        await database.AddData(entity);

        Assert.Single(database.Context.TestEntities);
    }
}
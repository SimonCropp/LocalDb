using System.Threading.Tasks;
using EfLocalDb;
using Xunit;

public class EfSnippetTests
{
    static SqlInstance<MyDbContext> sqlInstance;
    static EfSnippetTests()
    {
        sqlInstance = new(
            builder => new(builder.Options));
    }

    #region EfTest
    [Fact]
    public async Task TheTest()
    {
        #region EfBuildDatabase
        await using var database = await sqlInstance.Build();
        #endregion

        #region EfBuildContext
        await using (var data = database.NewDbContext())
        {
            #endregion
            TheEntity entity = new()
            {
                Property = "prop"
            };
            data.Add(entity);
            await data.SaveChangesAsync();
        }

        await using (var data = database.NewDbContext())
        {
            Assert.Single(data.TestEntities);
        }
        #endregion
    }

    [Fact]
    public async Task TheTestWithDbName()
    {
        #region EfWithDbName
        await using var database = await sqlInstance.Build("TheTestWithDbName");
        #endregion
        TheEntity entity = new()
        {
            Property = "prop"
        };
        await database.AddData(entity);

        Assert.Single(database.Context.TestEntities);
    }
}
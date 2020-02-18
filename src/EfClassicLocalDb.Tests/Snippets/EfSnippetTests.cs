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
        using var database = await sqlInstance.Build();
        using (var dbContext = database.NewDbContext())
        {
            var entity = new TheEntity
            {
                Property = "prop"
            };
            dbContext.TestEntities.Add(entity);
            dbContext.SaveChanges();
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
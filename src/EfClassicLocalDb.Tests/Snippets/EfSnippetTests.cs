using System.Threading.Tasks;
using EfLocalDb;
using Xunit;

public class EfSnippetTests
{
    static SqlInstance<MyDbContext> sqlInstance;
    static EfSnippetTests()
    {
        sqlInstance = new SqlInstance<MyDbContext>(
            builder => new MyDbContext(builder.Options));
    }

    [Fact]
    public async Task TheTest()
    {
        await using var database = await sqlInstance.Build();

        using (var dbContext = database.NewDbContext())
        {
            var entity = new TheEntity
            {
                Property = "prop"
            };
            dbContext.Add(entity);
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
        await using var database = await sqlInstance.Build("TheTestWithDbName");
        var entity = new TheEntity
        {
            Property = "prop"
        };
        await database.AddData(entity);

        Assert.Single(database.Context.TestEntities);
    }
}
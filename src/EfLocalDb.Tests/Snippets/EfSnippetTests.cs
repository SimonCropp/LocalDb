using EfLocalDb;

public class EfSnippetTests
{
    static SqlInstance<MyDbContext> sqlInstance;
    static EfSnippetTests()
    {
        sqlInstance = new SqlInstance<MyDbContext>(
            builder => new MyDbContext(builder.Options));
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
            var entity = new TheEntity
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
        var entity = new TheEntity
        {
            Property = "prop"
        };
        await database.AddData(entity);

        Assert.Single(database.Context.TestEntities);
    }
}
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

    #region EfTest

    [Fact]
    public async Task TheTest()
    {
        #region EfBuildDatabase
        using (var database = await sqlInstance.Build())
        {
            #endregion

            #region EfBuildContext
            using (var dbContext = database.NewDbContext())
            {
                #endregion
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
    }

    #endregion

    [Fact]
    public async Task TheTestWithDbName()
    {
        #region EfWithDbName
        using (var database = await sqlInstance.Build("TheTestWithDbName"))
        {
            #endregion
            var entity = new TheEntity
            {
                Property = "prop"
            };
            await database.AddData(entity);

            Assert.Single(database.Context.TestEntities);
        }
    }
}
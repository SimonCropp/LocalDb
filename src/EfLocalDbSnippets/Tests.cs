using System.Threading.Tasks;
using EfLocalDb;
using Xunit;

public class Tests
{
    #region EfTest

    [Fact]
    public async Task TheTest()
    {
        #region EfBuildLocalDbInstance
        var database = await SqlInstanceService<MyDbContext>.Build();
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

    #endregion

    [Fact]
    public async Task TheTestWithDbName()
    {
        #region EfWithDbName
        var database = await SqlInstanceService<MyDbContext>.Build("TheTestWithDbName");
        #endregion

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
}
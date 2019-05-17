using System.Threading.Tasks;
using EFLocalDb;
using Xunit;

public class Tests
{
    #region Test

    [Fact]
    public async Task TheTest()
    {
        #region BuildLocalDbInstance

        var localDb = await SqlInstanceService<MyDbContext>.Build();

        #endregion

        #region BuildDbContext

        using (var dbContext = localDb.NewDbContext())
        {
            #endregion
            var entity = new TestEntity
            {
                Property = "prop"
            };
            dbContext.Add(entity);
            dbContext.SaveChanges();
        }

        using (var dbContext = localDb.NewDbContext())
        {
            Assert.Single(dbContext.TestEntities);
        }
    }

    #endregion

    [Fact]
    public async Task TheTestWithDbName()
    {
        #region WithDbName

        var localDb = await SqlInstanceService<MyDbContext>.Build("TheTestWithDbName");

        #endregion

        using (var dbContext = localDb.NewDbContext())
        {
            var entity = new TestEntity
            {
                Property = "prop"
            };
            dbContext.Add(entity);
            dbContext.SaveChanges();
        }

        using (var dbContext = localDb.NewDbContext())
        {
            Assert.Single(dbContext.TestEntities);
        }
    }
}
using System.Threading.Tasks;
using EFLocalDb;
using Xunit;

public class Tests
{
    [Fact]
    public async Task ScopedDbContext()
    {
        var localDb = await LocalDb<ScopedDbContext>.Build(this);
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

    [Fact]
    public async Task Secondary()
    {
        var localDb = await LocalDb<SecondaryDbContext>.Build(this);
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

    [Fact]
    public async Task Simple()
    {
        var localDb = await LocalDb<TestDbContext>.Build(this);
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
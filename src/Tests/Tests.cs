using System.Threading.Tasks;
using EFLocalDb;
using Xunit;

public class Tests
{
    [Fact]
    public async Task ScopedDbContext()
    {
        var instance = new Instance<ScopedDbContext>(
            (connection, optionsBuilder) =>
            {
                using (var dbContext = new ScopedDbContext(optionsBuilder.Options))
                {
                    dbContext.Database.EnsureCreated();
                }
            },
            builder => new ScopedDbContext(builder.Options),
            instanceSuffix: "theSuffix");

        var localDb = await instance.Build(this);
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
        var instance = new Instance<SecondaryDbContext>(
            (connection, optionsBuilder) =>
            {
                using (var dbContext = new SecondaryDbContext(optionsBuilder.Options))
                {
                    dbContext.Database.EnsureCreated();
                }
            },
            builder => new SecondaryDbContext(builder.Options));
        var localDb = await instance.Build(this);
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
        var instance = new Instance<TestDbContext>(
            (connection, optionsBuilder) =>
            {
                using (var dbContext = new TestDbContext(optionsBuilder.Options))
                {
                    dbContext.Database.EnsureCreated();
                }
            },
            builder => new TestDbContext(builder.Options));
        var localDb = await instance.Build(this);
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
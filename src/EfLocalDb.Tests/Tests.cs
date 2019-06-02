using System;
using System.Linq;
using System.Threading.Tasks;
using ApprovalTests;
using EfLocalDb;
using Xunit;
using Xunit.Abstractions;

public class Tests :
    XunitLoggingBase
{
    [Fact]
    public async Task ScopedDbContext()
    {
        var instance = new SqlInstance<ScopedDbContext>(
            constructInstance: builder => new ScopedDbContext(builder.Options),
            instanceSuffix: "theSuffix");

        var database = await instance.Build();
        using (var dbContext = database.NewDbContext())
        {
            var entity = new TestEntity
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
    public async Task WithRebuildDbContext()
    {
        var instance1 = new SqlInstance<WithRebuildDbContext>(
            constructInstance: builder => new WithRebuildDbContext(builder.Options),
            requiresRebuild: dbContext => true);
        var database1 = await instance1.Build();
        using (var dbContext = database1.NewDbContext())
        {
            var entity = new TestEntity
            {
                Property = "prop"
            };
            dbContext.Add(entity);
            dbContext.SaveChanges();
        }

        using (var dbContext = database1.NewDbContext())
        {
            Assert.Single(dbContext.TestEntities);
        }

        var instance2 = new SqlInstance<WithRebuildDbContext>(constructInstance: builder => new WithRebuildDbContext(builder.Options),
            buildTemplate: x => throw new Exception(), requiresRebuild: dbContext => false);
        var database2 = await instance2.Build();
        using (var dbContext = database2.NewDbContext())
        {
            var entity = new TestEntity
            {
                Property = "prop"
            };
            dbContext.Add(entity);
            dbContext.SaveChanges();
        }

        using (var dbContext = database2.NewDbContext())
        {
            Assert.Single(dbContext.TestEntities);
        }
    }

    [Fact]
    public async Task Secondary()
    {
        var instance = new SqlInstance<SecondaryDbContext>(
            builder => new SecondaryDbContext(builder.Options));
        var database = await instance.Build();
        using (var dbContext = database.NewDbContext())
        {
            var entity = new TestEntity
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
    public void DuplicateDbContext()
    {
        Register();
        var exception = Assert.Throws<Exception>(Register);
        Approvals.Verify(exception.Message);
    }

    static void Register()
    {
        SqlInstanceService<DuplicateDbContext>.Register(
            constructInstance: builder => new DuplicateDbContext(builder.Options));
    }

    [Fact]
    public async Task Simple()
    {
        var instance = new SqlInstance<TestDbContext>(
            builder => new TestDbContext(builder.Options));
        var database = await instance.Build();
        using (var dbContext = database.NewDbContext())
        {
            var entity = new TestEntity
            {
                Property = "Item1"
            };
            dbContext.Add(entity);
            dbContext.SaveChanges();
        }

        await database.AddData(new TestEntity
        {
            Property = "Item2"
        });

        using (var dbContext = database.NewDbContext())
        {
            Assert.Equal(2, dbContext.TestEntities.Count());
        }
    }

    public Tests(ITestOutputHelper output) :
        base(output)
    {
    }
}
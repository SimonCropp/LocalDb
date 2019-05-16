using System;
using System.Threading.Tasks;
using ApprovalTests;
using EFLocalDb;
using Xunit;
using Xunit.Abstractions;

public class Tests :
    XunitLoggingBase
{
    [Fact]
    public async Task ScopedDbContext()
    {
        var instance = new SqlInstance<ScopedDbContext>(
            buildTemplate: (connection, builder) =>
            {
                using (var dbContext = new ScopedDbContext(builder.Options))
                {
                    dbContext.Database.EnsureCreated();
                }
            },
            constructInstance: builder => new ScopedDbContext(builder.Options),
            instanceSuffix: "theSuffix");

        var localDb = await instance.Build();
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
    public async Task WithRebuildDbContext()
    {
        var instance1 = new SqlInstance<WithRebuildDbContext>(
            buildTemplate: (connection, optionsBuilder) =>
            {
                using (var dbContext = new WithRebuildDbContext(optionsBuilder.Options))
                {
                    dbContext.Database.EnsureCreated();
                }
            },
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

        var instance2 = new SqlInstance<WithRebuildDbContext>(
            buildTemplate: (connection, optionsBuilder) => throw new Exception(),
            constructInstance: builder => new WithRebuildDbContext(builder.Options),
            requiresRebuild: dbContext => false);
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
            buildTemplate: (connection, builder) =>
            {
                using (var dbContext = new SecondaryDbContext(builder.Options))
                {
                    dbContext.Database.EnsureCreated();
                }
            },
            constructInstance: builder => new SecondaryDbContext(builder.Options));
        var localDb = await instance.Build();
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
    public void DuplicateDbContext()
    {
        Register();
        var exception = Assert.Throws<Exception>(Register);
        Approvals.Verify(exception.Message);
    }

    static void Register()
    {
        LocalDb<DuplicateDbContext>.Register(
            buildTemplate: (connection, builder) =>
            {
                using (var dbContext = new DuplicateDbContext(builder.Options))
                {
                    dbContext.Database.EnsureCreated();
                }
            },
            constructInstance: builder => new DuplicateDbContext(builder.Options));
    }

    [Fact]
    public async Task Simple()
    {
        var instance = new SqlInstance<TestDbContext>(
            (connection, optionsBuilder) =>
            {
                using (var dbContext = new TestDbContext(optionsBuilder.Options))
                {
                    dbContext.Database.EnsureCreated();
                }
            },
            builder => new TestDbContext(builder.Options));
        var localDb = await instance.Build();
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

    public Tests(ITestOutputHelper output) :
        base(output)
    {
    }
}
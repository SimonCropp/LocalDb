using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ApprovalTests;
using EfLocalDb;
using ObjectApproval;
using Xunit;
using Xunit.Abstractions;

public class Tests :
    XunitLoggingBase
{
    [Fact]
    public async Task SeedData()
    {
        var instance = new SqlInstance<ScopedDbContext>(
            constructInstance: builder => new ScopedDbContext(builder.Options),
            instanceSuffix: "SeedData");

        var entity = new TestEntity
        {
            Property = "prop"
        };
        var database = await instance.Build(new List<object>{entity});
        using (var dbContext = database.NewDbContext())
        {
            Assert.Single(dbContext.TestEntities);
        }
    }

    [Fact]
    public async Task AddData()
    {
        var instance = new SqlInstance<ScopedDbContext>(
            constructInstance: builder => new ScopedDbContext(builder.Options),
            instanceSuffix: "AddData");

        var database = await instance.Build();
        var entity = new TestEntity
        {
            Property = "prop"
        };
        await database.AddData(entity);

        using (var dbContext = database.NewDbContext())
        {
            Assert.Single(dbContext.TestEntities);
        }
    }

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
            await dbContext.SaveChangesAsync();
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
            await dbContext.SaveChangesAsync();
        }

        using (var dbContext = database1.NewDbContext())
        {
            Assert.Single(dbContext.TestEntities);
        }

        var instance2 = new SqlInstance<WithRebuildDbContext>(
            constructInstance: builder => new WithRebuildDbContext(builder.Options),
            buildTemplate: x => throw new Exception(), requiresRebuild: dbContext => false);
        var database2 = await instance2.Build();
        using (var dbContext = database2.NewDbContext())
        {
            var entity = new TestEntity
            {
                Property = "prop"
            };
            dbContext.Add(entity);
            await dbContext.SaveChangesAsync();
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
            await dbContext.SaveChangesAsync();
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
            await dbContext.SaveChangesAsync();
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

    [Fact]
    public async Task DbSettings()
    {
        var instance = new SqlInstance<TestDbContext>(
            builder => new TestDbContext(builder.Options));
        var database = await instance.Build();
        using (var connection = await database.OpenConnection())
        {
            var settings = DbPropertyReader.Read(connection, "Tests_DbSettings");
            ObjectApprover.VerifyWithJson(settings, s => s.Replace(Path.GetTempPath(), ""));
        }
    }

    public Tests(ITestOutputHelper output) :
        base(output)
    {
    }
}
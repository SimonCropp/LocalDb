using System;
using System.Collections.Generic;
using System.IO;
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
        using (var database = await instance.Build(new List<object> {entity}))
        {
            Assert.NotNull(database.Context.TestEntities.FindAsync(entity.Id));
        }
    }

    [Fact]
    public async Task AddData()
    {
        var instance = new SqlInstance<ScopedDbContext>(
            constructInstance: builder => new ScopedDbContext(builder.Options),
            instanceSuffix: "AddData");

        var entity = new TestEntity
        {
            Property = "prop"
        };
        using (var database = await instance.Build())
        {
            await database.AddData(entity);
            Assert.NotNull(database.Context.TestEntities.FindAsync(entity.Id));
        }
    }

    [Fact]
    public async Task ScopedDbContext()
    {
        var instance = new SqlInstance<ScopedDbContext>(
            constructInstance: builder => new ScopedDbContext(builder.Options),
            instanceSuffix: "theSuffix");

        var entity = new TestEntity
        {
            Property = "prop"
        };
        using (var database = await instance.Build(new List<object> {entity}))
        {
            Assert.NotNull(database.Context.TestEntities.FindAsync(entity.Id));
        }
    }

    [Fact]
    public async Task WithRebuildDbContext()
    {
        var instance1 = new SqlInstance<WithRebuildDbContext>(
            constructInstance: builder => new WithRebuildDbContext(builder.Options),
            requiresRebuild: dbContext => true);
        using (var database1 = await instance1.Build())
        {
            var entity = new TestEntity
            {
                Property = "prop"
            };
            await database1.AddData(entity);

            Assert.Single(database1.Context.TestEntities);
        }

        var instance2 = new SqlInstance<WithRebuildDbContext>(
            constructInstance: builder => new WithRebuildDbContext(builder.Options),
            buildTemplate: x => throw new Exception(), requiresRebuild: dbContext => false);
        using (var database2 = await instance2.Build())
        {
            var entity = new TestEntity
            {
                Property = "prop"
            };
            await database2.AddData(entity);
            Assert.Single(database2.Context.TestEntities);
        }
    }

    [Fact]
    public async Task Secondary()
    {
        var instance = new SqlInstance<SecondaryDbContext>(
            builder => new SecondaryDbContext(builder.Options));
        var entity = new TestEntity
        {
            Property = "prop"
        };
        var database = await instance.Build();
        using (var dbContext = database.NewDbContext())
        {
            dbContext.Add(entity);
            await dbContext.SaveChangesAsync();
        }

        using (var dbContext = database.NewDbContext())
        {
            Assert.NotNull(dbContext.TestEntities.FindAsync(entity.Id));
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
    public async Task NewDbContext()
    {
        var instance = new SqlInstance<TestDbContext>(
            builder => new TestDbContext(builder.Options));
        using (var database = await instance.Build())
        using (var dbContext = database.NewDbContext())
        {
            Assert.NotSame(database.Context, dbContext);
        }
    }

    [Fact]
    public async Task Simple()
    {
        var instance = new SqlInstance<TestDbContext>(
            builder => new TestDbContext(builder.Options));
        var entity = new TestEntity
        {
            Property = "Item1"
        };
        using (var database = await instance.Build(new List<object> {entity}))
        {
            Assert.NotNull(database.Context.TestEntities.FindAsync(entity.Id));
            var settings = DbPropertyReader.Read(database.Connection, "Tests_Simple");
            ObjectApprover.VerifyWithJson(settings, s => s.Replace(Path.GetTempPath(), ""));
        }
    }

    public Tests(ITestOutputHelper output) :
        base(output)
    {
    }
}
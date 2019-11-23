using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using EfLocalDb;
using VerifyXunit;
using Xunit;
using Xunit.Abstractions;

public class Tests :
    VerifyBase
{
    SqlInstance<TestDbContext> instance;

    [Fact]
    public async Task SeedData()
    {
        var entity = new TestEntity
        {
            Property = "prop"
        };
        await using var database = await instance.Build(new List<object> {entity});
        Assert.NotNull(await database.Context.TestEntities.FindAsync(entity.Id));
    }

    [Fact]
    public async Task AddData()
    {
        var entity = new TestEntity
        {
            Property = "prop"
        };
        await using var database = await instance.Build();
        await database.AddData(entity);
        Assert.NotNull(await database.Context.TestEntities.FindAsync(entity.Id));
    }

    [Fact]
    public async Task SuffixedContext()
    {
        var instance = new SqlInstance<TestDbContext>(
            constructInstance: builder => new TestDbContext(builder.Options),
            instanceSuffix: "theSuffix");

        var entity = new TestEntity
        {
            Property = "prop"
        };
        await using var database = await instance.Build(new List<object> {entity});
        Assert.NotNull(await database.Context.TestEntities.FindAsync(entity.Id));
    }

    [Fact]
    public async Task WithRebuildDbContext()
    {
        var dateTime = DateTime.Now;
        var instance1 = new SqlInstance<WithRebuildDbContext>(
            constructInstance: builder => new WithRebuildDbContext(builder.Options),
            timestamp: dateTime);
        await using (var database1 = await instance1.Build())
        {
            var entity = new TestEntity
            {
                Property = "prop"
            };
            await database1.AddData(entity);
        }

        var instance2 = new SqlInstance<WithRebuildDbContext>(
            constructInstance: builder => new WithRebuildDbContext(builder.Options),
            buildTemplate: x => throw new Exception(),
            timestamp: dateTime);
        await using var database2 = await instance2.Build();
        Assert.Empty(database2.Context.TestEntities);
    }

    [Fact]
    public async Task Secondary()
    {
        var entity = new TestEntity
        {
            Property = "prop"
        };
        await using var database = await instance.Build();
        await using (var dbContext = database.NewDbContext())
        {
            dbContext.Add(entity);
            await dbContext.SaveChangesAsync();
        }

        await using (var dbContext = database.NewDbContext())
        {
            Assert.NotNull(await dbContext.TestEntities.FindAsync(entity.Id));
        }
    }

    //TODO: should duplicate instances throw?
    //[Fact]
    //public void DuplicateDbContext()
    //{
    //    Register();
    //    var exception = Assert.Throws<Exception>(Register);
    //    await Verify(exception.Message);
    //}

    //static void Register()
    //{
    //    new SqlInstance<DuplicateDbContext>(
    //        constructInstance: builder => new DuplicateDbContext(builder.Options));
    //}

    [Fact]
    public async Task NewDbContext()
    {
        await using var database = await instance.Build();
        await using var dbContext = database.NewDbContext();
        Assert.NotSame(database.Context, dbContext);
    }

    [Fact]
    public async Task Simple()
    {
        var entity = new TestEntity
        {
            Property = "Item1"
        };
        await using var database = await instance.Build(new List<object> {entity});
        Assert.NotNull(await database.Context.TestEntities.FindAsync(entity.Id));
    }

    [Fact]
    public async Task WithRollback()
    {
        var entity = new TestEntity
        {
            Property = "prop"
        };
        await using var database1 = await instance.BuildWithRollback(new List<object> {entity});
        await using var database2 = await instance.BuildWithRollback();
        Assert.NotNull(await database1.Context.TestEntities.FindAsync(entity.Id));
        Assert.Empty(database2.Context.TestEntities.ToList());
    }

    [Fact]
    public async Task WithRollbackPerf()
    {
        await using (await instance.BuildWithRollback())
        {
        }

        var entity = new TestEntity
        {
            Property = "prop"
        };
        SqlDatabaseWithRollback<TestDbContext>? database2 = null;
        try
        {
            var stopwatch1 = Stopwatch.StartNew();
            database2 = await instance.BuildWithRollback();
            Trace.WriteLine(stopwatch1.ElapsedMilliseconds);
            await database2.AddData(entity);
        }
        finally
        {
            var stopwatch2 = Stopwatch.StartNew();
            database2?.Dispose();
            Trace.WriteLine(stopwatch2.ElapsedMilliseconds);
        }
    }

    public Tests(ITestOutputHelper output) :
        base(output)
    {
        instance = new SqlInstance<TestDbContext>(
            builder => new TestDbContext(builder.Options));
    }
}
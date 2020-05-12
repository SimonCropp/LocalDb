using System;
using System.Collections.Generic;
using System.IO;
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
        using var database = await instance.Build(new List<object> {entity});
        Assert.NotNull(await database.Context.TestEntities.FindAsync(entity.Id));
    }

    [Fact]
    public async Task AddData()
    {
        var entity = new TestEntity
        {
            Property = "prop"
        };
        using var database = await instance.Build();
        await database.AddData(entity);
        Assert.NotNull(await database.Context.TestEntities.FindAsync(entity.Id));
    }

    [Fact]
    public async Task SuffixedContext()
    {
        var instance = new SqlInstance<TestDbContext>(
            constructInstance: connection => new TestDbContext(connection),
            instanceSuffix: "theClassicSuffix");

        var entity = new TestEntity
        {
            Property = "prop"
        };
        using var database = await instance.Build(new List<object> {entity});
        Assert.NotNull(await database.Context.TestEntities.FindAsync(entity.Id));
    }
    [Fact]
    public async Task Defined_TimeStamp()
    {
        var dateTime = DateTime.Now;
        var instance = new SqlInstance<TestDbContext>(
            constructInstance: connection => new TestDbContext(connection),
            buildTemplate:async context =>
            {
                await context.CreateOnExistingDb();
            },
            timestamp: dateTime,
            instanceSuffix: "Defined_TimeStamp");

        using var database = await instance.Build();
        Assert.Equal(dateTime, File.GetCreationTime(instance.Wrapper.TemplateDataFile));
    }

    [Fact]
    public async Task Assembly_TimeStamp()
    {
        var instance = new SqlInstance<TestDbContext>(
            constructInstance: connection => new TestDbContext(connection),
            instanceSuffix: "Assembly_TimeStamp");

        using var database = await instance.Build();
        Assert.Equal(Timestamp.LastModified<Tests>(), File.GetCreationTime(instance.Wrapper.TemplateDataFile));
    }

    [Fact]
    public async Task Delegate_TimeStamp()
    {
        var instance = new SqlInstance<TestDbContext>(
            constructInstance: connection => new TestDbContext(connection),
            buildTemplate:async context =>
            {
                await context.CreateOnExistingDb();
            },
            instanceSuffix: "Delegate_TimeStamp");

        using var database = await instance.Build();
        Assert.Equal(Timestamp.LastModified<Tests>(), File.GetCreationTime(instance.Wrapper.TemplateDataFile));
    }

    //[Fact]
    //public async Task WithRebuildDbContext()
    //{
    //    var dateTime = DateTime.Now;
    //    var instance1 = new SqlInstance<WithRebuildDbContext>(
    //        constructInstance: connection => new WithRebuildDbContext(connection),
    //        timestamp: dateTime,
    //        instanceSuffix: "Classic");
    //    using (var database1 = await instance1.Build())
    //    {
    //        var entity = new TestEntity
    //        {
    //            Property = "prop"
    //        };
    //        await database1.AddData(entity);
    //    }

    //    var instance2 = new SqlInstance<WithRebuildDbContext>(
    //        constructInstance: connection => new WithRebuildDbContext(connection),
    //        buildTemplate: (WithRebuildDbContext x) => throw new Exception(),
    //        timestamp: dateTime,
    //        instanceSuffix: "Classic");
    //    using var database2 = await instance2.Build();
    //    Assert.Empty(database2.Context.TestEntities);
    //}

    [Fact]
    public async Task Secondary()
    {
        var entity = new TestEntity
        {
            Property = "prop"
        };
        using var database = await instance.Build();
        using (var data = database.NewDbContext())
        {
            data.TestEntities.Add(entity);
            await data.SaveChangesAsync();
        }

        using (var data = database.NewDbContext())
        {
            Assert.NotNull(await data.TestEntities.FindAsync(entity.Id));
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
        using var database = await instance.Build();
        using var data = database.NewDbContext();
        Assert.NotSame(database.Context, data);
    }

    [Fact]
    public async Task Simple()
    {
        var entity = new TestEntity
        {
            Property = "Item1"
        };
        using var database = await instance.Build(new List<object> {entity});
        Assert.NotNull(await database.Context.TestEntities.FindAsync(entity.Id));
    }

    //[Fact]
    //public async Task SuppliedTemplate()
    //{
    //    // The template has been pre-created with 2 test entities
    //    var templatePath = Path.Join(AppDomain.CurrentDomain.BaseDirectory, "suppliedTemplate.mdf");
    //    var logPath = Path.Join(AppDomain.CurrentDomain.BaseDirectory, "suppliedTemplate_log.ldf");

    //    var sqlInstance = new SqlInstance<TestDbContext>(
    //        constructInstance: x => new TestDbContext(x),
    //        templatePath: templatePath,
    //        logPath: logPath);
    //    using var database = await sqlInstance.Build();
    //    var context = database.Context;

    //    Assert.Equal(2, context.TestEntities.Count());
    //}

    //[Fact]
    //public async Task WithRollback()
    //{
    //    var entity = new TestEntity
    //    {
    //        Property = "prop"
    //    };
    //    using var database1 = await instance.BuildWithRollback(new List<object> {entity});
    //    using var database2 = await instance.BuildWithRollback();
    //    Assert.NotNull(await database1.Context.TestEntities.FindAsync(entity.Id));
    //    Assert.Empty(database2.Context.TestEntities.ToList());
    //}

    //[Fact]
    //public async Task WithRollbackPerf()
    //{
    //    using (await instance.BuildWithRollback())
    //    {
    //    }

    //    var entity = new TestEntity
    //    {
    //        Property = "prop"
    //    };
    //    SqlDatabaseWithRollback<TestDbContext>? database2 = null;
    //    try
    //    {
    //        var stopwatch1 = Stopwatch.StartNew();
    //        database2 = await instance.BuildWithRollback();
    //        Trace.WriteLine(stopwatch1.ElapsedMilliseconds);
    //        await database2.AddData(entity);
    //    }
    //    finally
    //    {
    //        var stopwatch2 = Stopwatch.StartNew();
    //        database2?.Dispose();
    //        Trace.WriteLine(stopwatch2.ElapsedMilliseconds);
    //    }
    //}

    public Tests(ITestOutputHelper output) :
        base(output)
    {
        instance = new SqlInstance<TestDbContext>(
            connection => new TestDbContext(connection),
            instanceSuffix:"Classic");
    }
}
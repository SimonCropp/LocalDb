using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EfLocalDb;
using Xunit;

public class Tests
{
    SqlInstance<TestDbContext> instance;
    bool callbackCalled;

    [Fact]
    public async Task SeedData()
    {
        var entity = new TestEntity
        {
            Property = "prop"
        };
        await using var database = await instance.Build(new List<object> {entity});
        Assert.NotNull(await database.Context.TestEntities.FindAsync(entity.Id));
        Assert.True(callbackCalled);
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
        Assert.True(callbackCalled);
    }

    [Fact]
    public async Task AddDataUntraccked()
    {
        var entity = new TestEntity
        {
            Property = "prop"
        };
        await using var database = await instance.Build();
        await database.AddDataUntracked(entity);
        Assert.NotNull(await database.Context.TestEntities.FindAsync(entity.Id));
        Assert.True(callbackCalled);
    }

    [Fact]
    public async Task AddDataMultiple()
    {
        var entity1 = new TestEntity
        {
            Id = 1,
            Property = "prop"
        };
        var entity2 = new TestEntity
        {
            Id = 2,
            Property = "prop"
        };
        await using var database = await instance.Build();
        await database.AddData(entity1, entity2);
        var testEntities = database.Context.TestEntities;
        Assert.NotNull(await testEntities.FindAsync(entity1.Id));
        Assert.NotNull(await testEntities.FindAsync(entity2.Id));
        Assert.True(callbackCalled);
    }

    [Fact]
    public async Task AddDataUntrackedMultiple()
    {
        var entity1 = new TestEntity
        {
            Id = 1,
            Property = "prop"
        };
        var entity2 = new TestEntity
        {
            Id = 2,
            Property = "prop"
        };
        await using var database = await instance.Build();
        await database.AddDataUntracked(entity1, entity2);
        var testEntities = database.Context.TestEntities;
        Assert.NotNull(await testEntities.FindAsync(entity1.Id));
        Assert.NotNull(await testEntities.FindAsync(entity2.Id));
        Assert.True(callbackCalled);
    }

    [Fact]
    public async Task SuffixedContext()
    {
        var instance = new SqlInstance<TestDbContext>(
            constructInstance: builder => new TestDbContext(builder.Options),
            storage: Storage.FromSuffix<TestDbContext>("theSuffix"));

        var entity = new TestEntity
        {
            Property = "prop"
        };
        await using var database = await instance.Build(new List<object> {entity});
        Assert.NotNull(await database.Context.TestEntities.FindAsync(entity.Id));
    }

    [Fact]
    public async Task SqlOptionsBuilder()
    {
        var optionsBuilderCalled = false;
        var instance = new SqlInstance<TestDbContext>(
            constructInstance: builder => new TestDbContext(builder.Options),
            sqlOptionsBuilder: builder => { optionsBuilderCalled = true; });

        var entity = new TestEntity
        {
            Property = "prop"
        };
        await using var database = await instance.Build(new List<object> {entity});
        Assert.NotNull(await database.Context.TestEntities.FindAsync(entity.Id));
        Assert.True(optionsBuilderCalled);
    }

    [Fact]
    public async Task BuildTemplate()
    {
        var instance = new SqlInstance<TestDbContext>(
            constructInstance: builder => new TestDbContext(builder.Options),
            buildTemplate: async context => { await context.Database.EnsureCreatedAsync(); },
            storage: Storage.FromSuffix<TestDbContext>("theSuffix"));

        var entity = new TestEntity
        {
            Property = "prop"
        };
        await using var database = await instance.Build(new List<object> {entity});
        Assert.NotNull(await database.Context.TestEntities.FindAsync(entity.Id));
    }

    [Fact]
    public async Task Defined_TimeStamp()
    {
        var dateTime = DateTime.Now;
        var instance = new SqlInstance<TestDbContext>(
            constructInstance: builder => new TestDbContext(builder.Options),
            buildTemplate: async context => { await context.Database.EnsureCreatedAsync(); },
            timestamp: dateTime,
            storage: Storage.FromSuffix<TestDbContext>("Defined_TimeStamp"));

        await using var database = await instance.Build();
        Assert.Equal(dateTime, File.GetCreationTime(instance.Wrapper.DataFile));
    }

    [Fact]
    public async Task Assembly_TimeStamp()
    {
        var instance = new SqlInstance<TestDbContext>(
            constructInstance: builder => new TestDbContext(builder.Options),
            storage: Storage.FromSuffix<TestDbContext>("Assembly_TimeStamp"));

        await using var database = await instance.Build();
        Assert.Equal(Timestamp.LastModified<Tests>(), File.GetCreationTime(instance.Wrapper.DataFile));
    }

    [Fact]
    public async Task Delegate_TimeStamp()
    {
        var instance = new SqlInstance<TestDbContext>(
            constructInstance: builder => new TestDbContext(builder.Options),
            buildTemplate: async context => { await context.Database.EnsureCreatedAsync(); },
            storage: Storage.FromSuffix<TestDbContext>("Delegate_TimeStamp"));

        await using var database = await instance.Build();
        Assert.Equal(Timestamp.LastModified<Tests>(), File.GetCreationTime(instance.Wrapper.DataFile));
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
        await using (var data = database.NewDbContext())
        {
            data.Add(entity);
            await data.SaveChangesAsync();
        }

        await using (var data = database.NewDbContext())
        {
            Assert.NotNull(await data.TestEntities.FindAsync(entity.Id));
        }

        Assert.True(callbackCalled);
    }

    [Fact]
    public async Task NewDbContext()
    {
        await using var database = await instance.Build();
        await using var data = database.NewDbContext();
        Assert.NotSame(database.Context, data);
        Assert.True(callbackCalled);
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
        Assert.True(callbackCalled);
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
        Assert.True(callbackCalled);
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
            if (database2 != null)
            {
                await database2.DisposeAsync();
            }

            Trace.WriteLine(stopwatch2.ElapsedMilliseconds);
        }

        Assert.True(callbackCalled);
    }

    public Tests()
    {
        instance = new SqlInstance<TestDbContext>(
            builder => new TestDbContext(builder.Options),
            callback: (connection, context) =>
            {
                callbackCalled = true;
                return Task.CompletedTask;
            });
    }
}
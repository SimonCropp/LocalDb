using System;
using System.Collections.Generic;
using System.IO;
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
        using var database = await instance.Build(new List<object> {entity});
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
        using var database = await instance.Build();
        await database.AddData(entity);
        Assert.NotNull(await database.Context.TestEntities.FindAsync(entity.Id));
        Assert.True(callbackCalled);
    }

    [Fact]
    public async Task AddDataUntracked()
    {
        var entity = new TestEntity
        {
            Property = "prop"
        };
        using var database = await instance.Build();
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
        using var database = await instance.Build();
        await database.AddData(entity1, entity2);
        var entities = database.Context.TestEntities;
        Assert.NotNull(await entities.FindAsync(entity1.Id));
        Assert.NotNull(await entities.FindAsync(entity2.Id));
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
        using var database = await instance.Build();
        await database.AddDataUntracked(entity1, entity2);
        var entities = database.Context.TestEntities;
        Assert.NotNull(await entities.FindAsync(entity1.Id));
        Assert.NotNull(await entities.FindAsync(entity2.Id));
        Assert.True(callbackCalled);
    }

    [Fact]
    public async Task SuffixedContext()
    {
        var instance = new SqlInstance<TestDbContext>(
            constructInstance: connection => new TestDbContext(connection),
            storage: Storage.FromSuffix<TestDbContext>($"theClassicSuffix{Environment.Version.Major}"));

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
            buildTemplate: async context => { await context.CreateOnExistingDb(); },
            timestamp: dateTime,
            storage: Storage.FromSuffix<TestDbContext>($"Defined_TimeStamp_Net{Environment.Version.Major}"));

        using var database = await instance.Build();
        Assert.Equal(dateTime, File.GetCreationTime(instance.Wrapper.DataFile));
    }

    [Fact]
    public async Task Assembly_TimeStamp()
    {
        var instance = new SqlInstance<TestDbContext>(
            constructInstance: connection => new TestDbContext(connection),
            storage: Storage.FromSuffix<TestDbContext>($"Assembly_TimeStamp{Environment.Version.Major}"));

        using var database = await instance.Build();
        Assert.Equal(Timestamp.LastModified<Tests>(), File.GetCreationTime(instance.Wrapper.DataFile));
    }

    [Fact]
    public async Task Delegate_TimeStamp()
    {
        var instance = new SqlInstance<TestDbContext>(
            constructInstance: connection => new TestDbContext(connection),
            buildTemplate: async context => { await context.CreateOnExistingDb(); },
            storage: Storage.FromSuffix<TestDbContext>($"Delegate_TimeStamp{Environment.Version.Major}"));

        using var database = await instance.Build();
        Assert.Equal(Timestamp.LastModified<Tests>(), File.GetCreationTime(instance.Wrapper.DataFile));
    }

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

        Assert.True(callbackCalled);
    }

    [Fact]
    public async Task NewDbContext()
    {
        using var database = await instance.Build();
        using var data = database.NewDbContext();
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
        using var database = await instance.Build(new List<object> {entity});
        Assert.NotNull(await database.Context.TestEntities.FindAsync(entity.Id));
        Assert.True(callbackCalled);
    }

    public Tests()
    {
        instance = new SqlInstance<TestDbContext>(
            connection => new TestDbContext(connection),
            storage: Storage.FromSuffix<TestDbContext>($"Classic{Environment.Version.Major}"),
            callback: (connection, context) =>
            {
                callbackCalled = true;
                return Task.CompletedTask;
            });
    }
}
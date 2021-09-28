using EfLocalDb;
using VerifyXunit;
using Xunit;

[UsesVerify]
public class Tests
{
    SqlInstance<TestDbContext> instance;
    bool callbackCalled;

    [Fact]
    public async Task SeedData()
    {
        TestEntity entity = new()
        {
            Property = "prop"
        };
        await using var database = await instance.Build(new List<object> {entity});
        Assert.True(await database.Exists(entity.Id));
        Assert.True(callbackCalled);
    }

    [Fact]
    public async Task AddData()
    {
        TestEntity entity = new()
        {
            Property = "prop"
        };
        await using var database = await instance.Build();
        await database.AddData(entity);
        Assert.True(await database.Exists(entity.Id));
        Assert.True(callbackCalled);
    }

    [Fact]
    public async Task AddDataUntracked()
    {
        TestEntity entity = new()
        {
            Property = "prop"
        };
        await using var database = await instance.Build();
        await database.AddDataUntracked(entity);
        Assert.True(await database.Exists(entity.Id));
        Assert.True(callbackCalled);
    }

    [Fact]
    public async Task ExistsT()
    {
        TestEntity entity = new()
        {
            Property = "prop"
        };
        await using var database = await instance.Build();
        await database.AddDataUntracked(entity);
        Assert.True(await database.Exists<TestEntity>(entity.Id));
    }

    [Fact]
    public async Task ExistsMissingT()
    {
        await using var database = await instance.Build();
        Assert.False(await database.Exists<TestEntity>(0));
    }

    [Fact]
    public async Task FindT()
    {
        TestEntity entity = new()
        {
            Property = "prop"
        };
        await using var database = await instance.Build();
        await database.AddDataUntracked(entity);
        await Verifier.Verify(database.Find<TestEntity>(entity.Id));
    }

    [Fact]
    public async Task FindMissingT()
    {
        await using var database = await instance.Build();
        await Verifier.ThrowsTask(() => database.Find<TestEntity>(0));
    }

    [Fact]
    public async Task Single()
    {
        TestEntity entity = new()
        {
            Property = "prop"
        };
        await using var database = await instance.Build();
        await database.AddDataUntracked(entity);
        await Verifier.Verify(database.Single<TestEntity>(_ => _.Id == entity.Id));
    }

    [Fact]
    public async Task SingleMissing()
    {
        await using var database = await instance.Build();
        await Verifier.ThrowsTask(() => database.Single<TestEntity>(entity => entity.Id == 10));
    }

    [Fact]
    public async Task CountT()
    {
        TestEntity entity = new()
        {
            Property = "prop"
        };
        await using var database = await instance.Build();
        await database.AddDataUntracked(entity);
        Assert.Equal(1, await database.Count<TestEntity>());
    }

    [Fact]
    public async Task CountMissingT()
    {
        await using var database = await instance.Build();
        Assert.Equal(0, await database.Count<TestEntity>());
    }

    [Fact]
    public async Task FindIncorrectTypeT()
    {
        await using var database = await instance.Build();
        await Verifier.ThrowsTask(() => database.Find<TestEntity>("key"));
    }

    [Fact]
    public async Task Exists()
    {
        TestEntity entity = new()
        {
            Property = "prop"
        };
        await using var database = await instance.Build();
        await database.AddDataUntracked(entity);
        Assert.True(await database.Exists(entity.Id));
    }

    [Fact]
    public async Task ExistsMissing()
    {
        await using var database = await instance.Build();
        Assert.False(await database.Exists(0));
    }

    [Fact]
    public async Task ExistsIncorrectType()
    {
        await using var database = await instance.Build();
        Assert.False(await database.Exists("key"));
    }

    [Fact]
    public async Task Find()
    {
        TestEntity entity = new()
        {
            Property = "prop"
        };
        await using var database = await instance.Build();
        await database.AddDataUntracked(entity);
        await Verifier.Verify(database.Find(entity.Id));
    }

    [Fact]
    public async Task FindMissing()
    {
        await using var database = await instance.Build();
        await Verifier.ThrowsTask(() => database.Find(0));
    }

    [Fact]
    public async Task FindIncorrectType()
    {
        await using var database = await instance.Build();
        await Verifier.ThrowsTask(() => database.Find("key"));
    }

    [Fact]
    public async Task AddDataMultiple()
    {
        TestEntity entity1 = new()
        {
            Property = "prop"
        };
        TestEntity entity2 = new()
        {
            Property = "prop"
        };
        await using var database = await instance.Build();
        await database.AddData(entity1, entity2);
        Assert.True(await database.Exists(entity1.Id));
        Assert.True(await database.Exists(entity2.Id));
        Assert.True(callbackCalled);
    }

    [Fact]
    public async Task AddDataMultipleMixed()
    {
        TestEntity entity1 = new()
        {
            Property = "prop"
        };
        TestEntity entity2 = new()
        {
            Property = "prop"
        };
        TestEntity entity3 = new()
        {
            Property = "prop"
        };
        await using var database = await instance.Build();
        await database.AddData(new List<object>{entity1, entity2}, entity3);
        var testEntities = database.Context.TestEntities;
        Assert.True(await database.Exists(entity1.Id));
        Assert.True(await database.Exists(entity2.Id));
        Assert.True(await database.Exists(entity3.Id));
        Assert.True(callbackCalled);
    }

    [Fact]
    public async Task AddDataUntrackedMultiple()
    {
        TestEntity entity1 = new()
        {
            Property = "prop"
        };
        TestEntity entity2 = new()
        {
            Property = "prop"
        };
        await using var database = await instance.Build();
        await database.AddDataUntracked(entity1, entity2);
        var testEntities = database.Context.TestEntities;
        Assert.True(await database.Exists(entity1.Id));
        Assert.True(await database.Exists(entity2.Id));
        Assert.True(callbackCalled);
    }

    [Fact]
    public async Task SuffixedContext()
    {
        SqlInstance<TestDbContext> instance = new(
            constructInstance: builder => new(builder.Options),
            storage: Storage.FromSuffix<TestDbContext>("theSuffix"));

        TestEntity entity = new()
        {
            Property = "prop"
        };
        await using var database = await instance.Build(new List<object> {entity});
        Assert.True(await database.Exists(entity.Id));
    }

    [Fact]
    public async Task SqlOptionsBuilder()
    {
        var optionsBuilderCalled = false;
        SqlInstance<TestDbContext> instance = new(
            constructInstance: builder => new(builder.Options),
            sqlOptionsBuilder: _ => { optionsBuilderCalled = true; });

        TestEntity entity = new()
        {
            Property = "prop"
        };
        await using var database = await instance.Build(new List<object> {entity});
        Assert.True(await database.Exists(entity.Id));
        Assert.True(optionsBuilderCalled);
    }

    [Fact]
    public async Task BuildTemplate()
    {
        SqlInstance<TestDbContext> instance = new(
            constructInstance: builder => new(builder.Options),
            buildTemplate: async context => { await context.Database.EnsureCreatedAsync(); },
            storage: Storage.FromSuffix<TestDbContext>("theSuffix"));

        TestEntity entity = new()
        {
            Property = "prop"
        };
        await using var database = await instance.Build(new List<object> {entity});
        Assert.True(await database.Exists(entity.Id));
    }

    [Fact]
    public async Task Defined_TimeStamp()
    {
        var dateTime = DateTime.Now;
        SqlInstance<TestDbContext> instance = new(
            constructInstance: builder => new(builder.Options),
            buildTemplate: async context => { await context.Database.EnsureCreatedAsync(); },
            timestamp: dateTime,
            storage: Storage.FromSuffix<TestDbContext>("Defined_TimeStamp"));

        await using var database = await instance.Build();
        Assert.Equal(dateTime, File.GetCreationTime(instance.Wrapper.DataFile));
    }

    [Fact]
    public async Task Assembly_TimeStamp()
    {
        SqlInstance<TestDbContext> instance = new(
            constructInstance: builder => new(builder.Options),
            storage: Storage.FromSuffix<TestDbContext>("Assembly_TimeStamp"));

        await using var database = await instance.Build();
        Assert.Equal(Timestamp.LastModified<Tests>(), File.GetCreationTime(instance.Wrapper.DataFile));
    }

    [Fact]
    public async Task Delegate_TimeStamp()
    {
        SqlInstance<TestDbContext> instance = new(
            constructInstance: builder => new(builder.Options),
            buildTemplate: async context => { await context.Database.EnsureCreatedAsync(); },
            storage: Storage.FromSuffix<TestDbContext>("Delegate_TimeStamp"));

        await using var database = await instance.Build();
        Assert.Equal(Timestamp.LastModified<Tests>(), File.GetCreationTime(instance.Wrapper.DataFile));
    }

    [Fact]
    public async Task WithRebuildDbContext()
    {
        var dateTime = DateTime.Now;
        SqlInstance<WithRebuildDbContext> instance1 = new(
            constructInstance: builder => new(builder.Options),
            timestamp: dateTime);
        await using (var database1 = await instance1.Build())
        {
            TestEntity entity = new()
            {
                Property = "prop"
            };
            await database1.AddData(entity);
        }

        SqlInstance<WithRebuildDbContext> instance2 = new(
            constructInstance: builder => new(builder.Options),
            buildTemplate: _ => throw new(),
            timestamp: dateTime);
        await using var database2 = await instance2.Build();
        Assert.Empty(database2.Context.TestEntities);
    }

    [Fact]
    public async Task Secondary()
    {
        TestEntity entity = new()
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
        TestEntity entity = new()
        {
            Property = "Item1"
        };
        await using var database = await instance.Build(new List<object> {entity});
        Assert.True(await database.Exists(entity.Id));
        Assert.True(callbackCalled);
    }

    [Fact]
    public async Task SimpleContext()
    {
        TestEntity entity = new()
        {
            Property = "Item1"
        };
        await using var context = await instance.BuildContext(new List<object> {entity});
        Assert.NotNull(await context.TestEntities.FindAsync(entity.Id));
        Assert.True(callbackCalled);
    }

    public Tests()
    {
        instance = new(
            builder => new(builder.Options),
            callback: (_, _) =>
            {
                callbackCalled = true;
                return Task.CompletedTask;
            });
    }
}
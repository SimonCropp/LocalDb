using EfLocalDb;

[UsesVerify]
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
        Assert.True(await database.Exists(entity.Id));
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
        Assert.True(await database.Exists(entity.Id));
        Assert.True(callbackCalled);
    }

    [Fact]
    public async Task AddDataUntracked()
    {
        var entity = new TestEntity
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
        var entity = new TestEntity
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
        var entity = new TestEntity
        {
            Property = "prop"
        };
        await using var database = await instance.Build();
        await database.AddDataUntracked(entity);
        await Verify(database.Find<TestEntity>(entity.Id));
    }

    [Fact]
    public async Task FindMissingT()
    {
        await using var database = await instance.Build();
        await ThrowsTask(() => database.Find<TestEntity>(0));
    }

    [Fact]
    public async Task Single()
    {
        var entity = new TestEntity
        {
            Property = "prop"
        };
        await using var database = await instance.Build();
        await database.AddDataUntracked(entity);
        await Verify(database.Single<TestEntity>(_ => _.Id == entity.Id));
    }

    [Fact]
    public async Task SingleMissing()
    {
        await using var database = await instance.Build();
        await ThrowsTask(() => database.Single<TestEntity>(entity => entity.Id == 10));
    }

    [Fact]
    public async Task CountT()
    {
        var entity = new TestEntity
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
        await ThrowsTask(() => database.Find<TestEntity>("key"));
    }

    [Fact]
    public async Task Exists()
    {
        var entity = new TestEntity
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
        var entity = new TestEntity
        {
            Property = "prop"
        };
        await using var database = await instance.Build();
        await database.AddDataUntracked(entity);
        await Verify(database.Find(entity.Id));
    }

    [Fact]
    public async Task FindMissing()
    {
        await using var database = await instance.Build();
        await ThrowsTask(() => database.Find(0));
    }

    [Fact]
    public async Task FindIncorrectType()
    {
        await using var database = await instance.Build();
        await ThrowsTask(() => database.Find("key"));
    }

    [Fact]
    public async Task AddDataMultiple()
    {
        var entity1 = new TestEntity
        {
            Property = "prop"
        };
        var entity2 = new TestEntity
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
        var entity1 = new TestEntity
        {
            Property = "prop"
        };
        var entity2 = new TestEntity
        {
            Property = "prop"
        };
        var entity3 = new TestEntity
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
        var entity1 = new TestEntity
        {
            Property = "prop"
        };
        var entity2 = new TestEntity
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
        var instance = new SqlInstance<TestDbContext>(constructInstance: builder => new TestDbContext(builder.Options), storage: Storage.FromSuffix<TestDbContext>("theSuffix"));

        var entity = new TestEntity
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
        var instance = new SqlInstance<TestDbContext>(constructInstance: builder => new TestDbContext(builder.Options), sqlOptionsBuilder: _ => { optionsBuilderCalled = true; });

        var entity = new TestEntity
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
        var instance = new SqlInstance<TestDbContext>(constructInstance: builder => new TestDbContext(builder.Options), buildTemplate: async context => { await context.Database.EnsureCreatedAsync(); }, storage: Storage.FromSuffix<TestDbContext>("theSuffix"));

        var entity = new TestEntity
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
        var instance = new SqlInstance<TestDbContext>(constructInstance: builder => new TestDbContext(builder.Options), buildTemplate: async context => { await context.Database.EnsureCreatedAsync(); }, timestamp: dateTime, storage: Storage.FromSuffix<TestDbContext>("Defined_TimeStamp"));

        await using var database = await instance.Build();
        Assert.Equal(dateTime, File.GetCreationTime(instance.Wrapper.DataFile));
    }

    [Fact]
    public async Task Assembly_TimeStamp()
    {
        var instance = new SqlInstance<TestDbContext>(constructInstance: builder => new TestDbContext(builder.Options), storage: Storage.FromSuffix<TestDbContext>("Assembly_TimeStamp"));

        await using var database = await instance.Build();
        Assert.Equal(Timestamp.LastModified<Tests>(), File.GetCreationTime(instance.Wrapper.DataFile));
    }

    [Fact]
    public async Task Delegate_TimeStamp()
    {
        var instance = new SqlInstance<TestDbContext>(constructInstance: builder => new TestDbContext(builder.Options), buildTemplate: async context => { await context.Database.EnsureCreatedAsync(); }, storage: Storage.FromSuffix<TestDbContext>("Delegate_TimeStamp"));

        await using var database = await instance.Build();
        Assert.Equal(Timestamp.LastModified<Tests>(), File.GetCreationTime(instance.Wrapper.DataFile));
    }

    [Fact]
    public async Task WithRebuildDbContext()
    {
        var dateTime = DateTime.Now;
        var instance1 = new SqlInstance<WithRebuildDbContext>(constructInstance: builder => new WithRebuildDbContext(builder.Options), timestamp: dateTime);
        await using (var database1 = await instance1.Build())
        {
            var entity = new TestEntity
            {
                Property = "prop"
            };
            await database1.AddData(entity);
        }

        var instance2 = new SqlInstance<WithRebuildDbContext>(constructInstance: builder => new WithRebuildDbContext(builder.Options), buildTemplate: _ => throw new Exception(), timestamp: dateTime);
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
        Assert.True(await database.Exists(entity.Id));
        Assert.True(callbackCalled);
    }

    [Fact]
    public async Task SimpleContext()
    {
        var entity = new TestEntity
        {
            Property = "Item1"
        };
        await using var context = await instance.BuildContext(new List<object> {entity});
        Assert.NotNull(await context.TestEntities.FindAsync(entity.Id));
        Assert.True(callbackCalled);
    }

    public Tests()
    {
        instance = new SqlInstance<TestDbContext>(
            builder => new TestDbContext(builder.Options),
            callback: (_, _) =>
            {
                callbackCalled = true;
                return Task.CompletedTask;
            });
    }
}
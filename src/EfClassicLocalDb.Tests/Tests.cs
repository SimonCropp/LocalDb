using System.Data.SqlClient;

[TestFixture]
public class Tests
{
    static SqlInstance<TestDbContext> instance;
    static bool callbackCalled;

    [Test]
    public async Task SeedData()
    {
        var entity = new TestEntity
        {
            Property = "prop"
        };
        using var database = await instance.Build([entity]);
        NotNull(await database.Context.TestEntities.FindAsync(entity.Id));
        True(callbackCalled);
    }

    [Test]
    public async Task ServiceScope()
    {
        static void Add(List<object?> objects, IServiceProvider provider)
        {
            objects.Add(provider.GetService<SqlConnection>());
            objects.Add(provider.GetService<TestDbContext>());
        }

        using var database = await instance.Build();
        await using var asyncScope = database.CreateAsyncScope();
        await using var providerAsyncScope = ((IServiceProvider)database).CreateAsyncScope();
        await using var scopeFactoryAsyncScope = ((IServiceScopeFactory)database).CreateAsyncScope();
        using var scope = database.CreateScope();
        var list = new List<object?>();
        Add(list, database);
        Add(list, scope.ServiceProvider);
        Add(list, asyncScope.ServiceProvider);
        Add(list, providerAsyncScope.ServiceProvider);
        Add(list, scopeFactoryAsyncScope.ServiceProvider);

        for (var outerIndex = 0; outerIndex < list.Count; outerIndex++)
        {
            var item = list[outerIndex];
            NotNull(item);
            for (var innerIndex = 0; innerIndex < list.Count; innerIndex++)
            {
                if (innerIndex == outerIndex)
                {
                    continue;
                }
                var nested = list[innerIndex];
                AreNotEqual(item, nested);
            }
        }
    }

    [Test]
    public async Task AddData()
    {
        var entity = new TestEntity
        {
            Property = "prop"
        };
        using var database = await instance.Build();
        await database.AddData(entity);
        NotNull(await database.Context.TestEntities.FindAsync(entity.Id));
        True(callbackCalled);
    }

    [Test]
    public async Task AddDataUntracked()
    {
        var entity = new TestEntity
        {
            Property = "prop"
        };
        using var database = await instance.Build();
        await database.AddDataUntracked(entity);
        NotNull(await database.Context.TestEntities.FindAsync(entity.Id));
        True(callbackCalled);
    }

    [Test]
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
        NotNull(await entities.FindAsync(entity1.Id));
        NotNull(await entities.FindAsync(entity2.Id));
        True(callbackCalled);
    }

    [Test]
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
        NotNull(await entities.FindAsync(entity1.Id));
        NotNull(await entities.FindAsync(entity2.Id));
        True(callbackCalled);
    }

    [Test]
    public async Task SuffixedContext()
    {
        var instance = new SqlInstance<TestDbContext>(
            connection => new(connection),
            storage: Storage.FromSuffix<TestDbContext>($"theClassicSuffix{Environment.Version.Major}"));

        var entity = new TestEntity
        {
            Property = "prop"
        };
        using var database = await instance.Build([entity]);
        NotNull(await database.Context.TestEntities.FindAsync(entity.Id));
    }

#if DEBUG
    [Test]
    public async Task Defined_TimeStamp()
    {
        var dateTime = DateTime.Now;
        var instance = new SqlInstance<TestDbContext>(
            connection => new(connection),
            context => context.CreateOnExistingDb(),
            timestamp: dateTime,
            storage: Storage.FromSuffix<TestDbContext>($"Defined_TimeStamp_Net{Environment.Version.Major}"));

        using var database = await instance.Build();
        AreEqual(dateTime, File.GetCreationTime(instance.Wrapper.DataFile));
    }

    [Test]
    public async Task Assembly_TimeStamp()
    {
        var instance = new SqlInstance<TestDbContext>(
            connection => new(connection),
            storage: Storage.FromSuffix<TestDbContext>($"Assembly_TimeStamp{Environment.Version.Major}"));

        using var database = await instance.Build();
        AreEqual(Timestamp.LastModified<Tests>(), File.GetCreationTime(instance.Wrapper.DataFile));
    }

    [Test]
    public async Task Delegate_TimeStamp()
    {
        var instance = new SqlInstance<TestDbContext>(
            connection => new(connection),
            context => context.CreateOnExistingDb(),
            Storage.FromSuffix<TestDbContext>($"Delegate_TimeStamp{Environment.Version.Major}"));

        using var database = await instance.Build();
        AreEqual(Timestamp.LastModified<Tests>(), File.GetCreationTime(instance.Wrapper.DataFile));
    }
#endif

    [Test]
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
            NotNull(await data.TestEntities.FindAsync(entity.Id));
        }

        True(callbackCalled);
    }

    [Test]
    public async Task NewDbContext()
    {
        using var database = await instance.Build();
        using var data = database.NewDbContext();
        AreNotEqual(database.Context, data);
        True(callbackCalled);
    }

    [Test]
    public async Task Simple()
    {
        var entity = new TestEntity
        {
            Property = "Item1"
        };
        using var database = await instance.Build([entity]);
        NotNull(await database.Context.TestEntities.FindAsync(entity.Id));
        True(callbackCalled);
    }

    static Tests() =>
        instance = new(
            connection => new(connection),
            storage: Storage.FromSuffix<TestDbContext>($"Classic{Environment.Version.Major}"),
            callback: (_, _) =>
            {
                callbackCalled = true;
                return Task.CompletedTask;
            });
}
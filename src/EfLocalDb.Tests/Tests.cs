#pragma warning disable CS0612 // Type or member is obsolete
using NetTopologySuite.Geometries;

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
        await using var database = await instance.Build([entity]);
        NotNull(await database.Context.FindAsync<TestEntity>(entity.Id));
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

        await using var database = await instance.Build();
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
        await using var database = await instance.Build();
        await database.AddData(entity);
        NotNull(await database.Context.FindAsync<TestEntity>(entity.Id));
        True(callbackCalled);
    }

    [Test]
    public async Task ThrowOnSaveForNoData()
    {
        await using var database = await instance.Build();
        await ThrowsTask(() => database.SaveChangesAsync())
            .IgnoreStackTrace();
    }

    [Test]
    public async Task RemoveData()
    {
        var entity = new TestEntity
        {
            Property = "prop"
        };
        await using var database = await instance.Build();
        await database.AddData(entity);
        await database.RemoveData(entity);
        Null(await database.Context.FindAsync<TestEntity>(entity.Id));
        True(callbackCalled);
    }

    [Test]
    public async Task AddDataUntracked()
    {
        var entity = new TestEntity
        {
            Property = "prop"
        };
        await using var database = await instance.Build();
        await database.AddDataUntracked(entity);
        NotNull(await database.Context.FindAsync<TestEntity>(entity.Id));
        True(callbackCalled);
    }

    [Test]
    public async Task RemoveDataUntracked()
    {
        var entity = new TestEntity
        {
            Property = "prop"
        };
        await using var database = await instance.Build();
        await database.AddDataUntracked(entity);
        await database.RemoveDataUntracked(entity);
        Null(await database.Context.FindAsync<TestEntity>(entity.Id));
        True(callbackCalled);
    }

    [Test]
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

    [Test]
    public async Task SingleIgnoreFilters()
    {
        var entity = new TestEntity
        {
            Property = "filtered"
        };
        await using var database = await instance.Build();
        await database.AddDataUntracked(entity);
        await Verify(database.SingleIgnoreFilters<TestEntity>(_ => _.Id == entity.Id));
    }

    [Test]
    public async Task SingleMissing()
    {
        await using var database = await instance.Build();
        await ThrowsTask(() => database.Single<TestEntity>(entity => entity.Id == 10));
    }

    [Test]
    public async Task SingleMissingIgnoreFilters()
    {
        await using var database = await instance.Build();
        await ThrowsTask(() => database.SingleIgnoreFilters<TestEntity>(entity => entity.Id == 10));
    }

    [Test]
    public async Task Any()
    {
        var entity = new TestEntity
        {
            Property = "prop"
        };
        await using var database = await instance.Build();
        await database.AddDataUntracked(entity);
        await Verify(database.Any<TestEntity>(_ => _.Id == entity.Id));
    }

    [Test]
    public async Task AnyIgnoreFilters()
    {
        var entity = new TestEntity
        {
            Property = "filtered"
        };
        await using var database = await instance.Build();
        await database.AddDataUntracked(entity);
        await Verify(database.AnyIgnoreFilters<TestEntity>(_ => _.Id == entity.Id));
    }

    [Test]
    public async Task AnyMissing()
    {
        await using var database = await instance.Build();
        await Verify(database.Any<TestEntity>(entity => entity.Id == 10));
    }

    [Test]
    public async Task AnyMissingIgnoreFilters()
    {
        await using var database = await instance.Build();
        await Verify(database.AnyIgnoreFilters<TestEntity>(entity => entity.Id == 10));
    }

    [Test]
    public async Task CountT()
    {
        var entity = new TestEntity
        {
            Property = "prop"
        };
        await using var database = await instance.Build();
        await database.AddDataUntracked(entity);
        AreEqual(1, await database.Count<TestEntity>());
    }

    [Test]
    public async Task CountMissingT()
    {
        await using var database = await instance.Build();
        AreEqual(0, await database.Count<TestEntity>());
    }

    [Test]
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
        NotNull(await database.Context.FindAsync<TestEntity>(entity1.Id));
        NotNull(await database.Context.FindAsync<TestEntity>(entity2.Id));
        True(callbackCalled);
    }

    [Test]
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
        await database.AddData(
            new List<object>
            {
                entity1,
                entity2
            },
            entity3);
        NotNull(await database.Context.FindAsync<TestEntity>(entity1.Id));
        NotNull(await database.Context.FindAsync<TestEntity>(entity2.Id));
        NotNull(await database.Context.FindAsync<TestEntity>(entity3.Id));
        True(callbackCalled);
    }

    [Test]
    public async Task RemoveDataMultipleMixed()
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
        await database.AddData(
            new List<object>
            {
                entity1,
                entity2
            },
            entity3);
        await database.RemoveData(
            new List<object>
            {
                entity1,
                entity2
            },
            entity3);
        Null(await database.Context.FindAsync<TestEntity>(entity1.Id));
        Null(await database.Context.FindAsync<TestEntity>(entity2.Id));
        Null(await database.Context.FindAsync<TestEntity>(entity3.Id));
        True(callbackCalled);
    }

    [Test]
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
        NotNull(await database.Context.FindAsync<TestEntity>(entity1.Id));
        NotNull(await database.Context.FindAsync<TestEntity>(entity2.Id));
        True(callbackCalled);
    }

    [Test]
    public async Task RemoveDataUntrackedMultiple()
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
        await database.RemoveDataUntracked(entity1, entity2);
        Null(await database.Context.FindAsync<TestEntity>(entity1.Id));
        Null(await database.Context.FindAsync<TestEntity>(entity2.Id));
        True(callbackCalled);
    }

    [Test]
    public async Task SuffixedContext()
    {
        var instance = new SqlInstance<TestDbContext>(
            builder => new(builder.Options),
            storage: Storage.FromSuffix<TestDbContext>("theSuffix"));

        var entity = new TestEntity
        {
            Property = "prop"
        };
        await using var database = await instance.Build([entity]);
        NotNull(await database.Context.FindAsync<TestEntity>(entity.Id));
    }

    [Test]
    public async Task SqlOptionsBuilder()
    {
        var optionsBuilderCalled = false;
        var instance = new SqlInstance<TestDbContext>(
            builder => new(builder.Options),
            sqlOptionsBuilder: _ =>
            {
                optionsBuilderCalled = true;
            },
            storage: Storage.FromSuffix<TestDbContext>("SqlOptionsBuilder"));

        var entity = new TestEntity
        {
            Property = "prop"
        };
        await using var database = await instance.Build([entity]);
        NotNull(await database.Context.FindAsync<TestEntity>(entity.Id));
        True(optionsBuilderCalled);
    }

    [Test]
    public async Task Defined_TimeStamp()
    {
        var dateTime = DateTime.Now;
        var instance = new SqlInstance<TestDbContext>(
            builder => new(builder.Options),
            async context =>
            {
                await context.Database.EnsureCreatedAsync();
            },
            timestamp: dateTime,
            storage: Storage.FromSuffix<TestDbContext>("Defined_TimeStamp"));

        await using var database = await instance.Build();
        AreEqual(dateTime, File.GetCreationTime(instance.Wrapper.DataFile));
    }

    [Test]
    public async Task Assembly_TimeStamp()
    {
        var instance = new SqlInstance<TestDbContext>(
            builder => new(builder.Options),
            storage: Storage.FromSuffix<TestDbContext>("Assembly_TimeStamp"));

        await using var database = await instance.Build();
        AreEqual(Timestamp.LastModified<Tests>(), File.GetCreationTime(instance.Wrapper.DataFile));
    }

    [Test]
    public async Task Delegate_TimeStamp()
    {
        var instance = new SqlInstance<TestDbContext>(
            builder => new(builder.Options),
            async context =>
            {
                await context.Database.EnsureCreatedAsync();
            },
            Storage.FromSuffix<TestDbContext>("Delegate_TimeStamp"));

        await using var database = await instance.Build();
        AreEqual(Timestamp.LastModified<Tests>(), File.GetCreationTime(instance.Wrapper.DataFile));
    }

    // [Test]
    // public async Task WithRebuildDbContext()
    // {
    //     var dateTime = DateTime.Now;
    //     var instance1 = new SqlInstance<WithRebuildDbContext>(
    //         builder => new(builder.Options),
    //         timestamp: dateTime);
    //     await using (var database1 = await instance1.Build())
    //     {
    //         var entity = new TestEntity
    //         {
    //             Property = "prop"
    //         };
    //         await database1.AddData(entity);
    //     }
    //     instance1.Cleanup(ShutdownMode.UseSqlShutdown);
    //
    //     var instance2 = new SqlInstance<WithRebuildDbContext>(
    //         builder => new(builder.Options),
    //         _ => throw new(),
    //         timestamp: dateTime);
    //     await using var database2 = await instance2.Build();
    //     IsEmpty(database2.Context.TestEntities);
    // }

    [Test]
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
            NotNull(await data.TestEntities.FindAsync(entity.Id));
        }

        True(callbackCalled);
    }

    [Test]
    public async Task OwnedDbContext()
    {
        var entity = new TestEntity
        {
            Property = "prop"
        };
        await using var database = await instance.Build();
        await using (var data = database.NewConnectionOwnedDbContext())
        {
            data.Add(entity);
            await data.SaveChangesAsync();
        }

        await using (var data = database.NewConnectionOwnedDbContext())
        {
            NotNull(await data.TestEntities.FindAsync(entity.Id));
        }

        True(callbackCalled);
    }

    [Test]
    public async Task NewDbContext()
    {
        await using var database = await instance.Build();
        await using var data = database.NewDbContext();
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
        await using var database = await instance.Build([entity]);
        NotNull(await database.Context.FindAsync<TestEntity>(entity.Id));
        True(callbackCalled);
    }

    [Test]
    public async Task SimpleContext()
    {
        var entity = new TestEntity
        {
            Property = "Item1"
        };
        await using var context = await instance.BuildContext([entity]);
        NotNull(await context.TestEntities.FindAsync(entity.Id));
        True(callbackCalled);
    }

    static Tests() =>
        instance = new(
            builder => new(builder.Options),
            callback: (_, _) =>
            {
                callbackCalled = true;
                return Task.CompletedTask;
            });

    [Test]
    public async Task BuildTemplate()
    {
        var instance = new SqlInstance<TestDbContext>(
            builder => new(builder.Options),
            async context =>
            {
                await context.Database.EnsureCreatedAsync();
            },
            storage: Storage.FromSuffix<TestDbContext>("BuildTemplate"));

        var entity = new TestEntity
        {
            Property = "prop"
        };
        await using var database = await instance.Build([entity]);
        NotNull(await database.Context.FindAsync<TestEntity>(entity.Id));
    }

    [Test]
    public async Task FiltersDisabled()
    {
        var entity = new TestEntity
        {
            Property = "filtered"
        };
        await using var database = await instance.Build(data: [entity]);
        var context = database.NoTrackingContext;
        QueryFilter.Disable();
        NotNull(await context.FindAsync<TestEntity>(entity.Id));
    }

    [Test]
    public async Task FiltersDisabled_Multiple()
    {
        var entity = new TestEntity
        {
            Property = "filtered"
        };
        await using var database = await instance.Build(data: [entity]);
        var context = database.NoTrackingContext;
        QueryFilter.Disable();
        NotNull(await context.FindAsync<TestEntity>(entity.Id));
        NotNull(await context.FindAsync<TestEntity>(entity.Id));
    }

    [Test]
    public async Task FiltersDisabled_AfterWithEnabled()
    {
        var entity = new TestEntity
        {
            Property = "filtered"
        };
        await using var database = await instance.Build(data: [entity]);
        var context = database.NoTrackingContext;
        Null(await context.FindAsync<TestEntity>(entity.Id));
        QueryFilter.Disable();
        NotNull(await context.FindAsync<TestEntity>(entity.Id));
    }

    [Test]
    public async Task FiltersEnabled_AfterWithDisabled()
    {
        var entity = new TestEntity
        {
            Property = "filtered"
        };
        await using var database = await instance.Build(data: [entity]);
        var context = database.NoTrackingContext;
        QueryFilter.Disable();
        NotNull(await context.FindAsync<TestEntity>(entity.Id));
        QueryFilter.Enable();
        Null(await context.FindAsync<TestEntity>(entity.Id));
    }

    [Test]
    public void EntityWithGeometry()
    {
        SqlInstance<GeometryDbContext> sqlInstance = new (builder => new(builder.Options),
                sqlOptionsBuilder: sb => sb.UseNetTopologySuite());
        NotNull(sqlInstance); 
    }

    public class GeometryDbContext(DbContextOptions options) : DbContext(options)
    {
        public DbSet<GeometryEntity> GeometryEntities { get; set; } = null!;
    }

    public class GeometryEntity
    {
        public int Id { get; set; }
        public Geometry? Location { get; set; }
    }
}

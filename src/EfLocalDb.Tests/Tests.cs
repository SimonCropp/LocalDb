using EfLocalDb;
using Microsoft.Data.SqlClient;

[UsesVerify]
public class Tests
{
    static SqlInstance<TestDbContext> instance;
    static bool callbackCalled;

    [Fact]
    public async Task SeedData()
    {
        var entity = new TestEntity
        {
            Property = "prop"
        };
        await using var database = await instance.Build(new List<object>
        {
            entity
        });
        Assert.True(await database.Exists(entity.Id));
        Assert.True(callbackCalled);
    }

    [Fact]
    public async Task ServiceScope()
    {
        void Add(List<object?> objects, IServiceProvider provider)
        {
            objects.Add(provider.GetService<DataSqlConnection>());
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
            Assert.NotNull(item);
            for (var innerIndex = 0; innerIndex < list.Count; innerIndex++)
            {
                if (innerIndex == outerIndex)
                {
                    continue;
                }
                var nested = list[innerIndex];
                Assert.NotSame(item, nested);
            }
        }
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
    public async Task ThrowOnSaveForNoData()
    {
        await using var database = await instance.Build();
        await ThrowsTask(() => database.SaveChangesAsync())
            .IgnoreStackTrace();
    }

    [Fact]
    public async Task RemoveData()
    {
        var entity = new TestEntity
        {
            Property = "prop"
        };
        await using var database = await instance.Build();
        await database.AddData(entity);
        await database.RemoveData(entity);
        Assert.False(await database.Exists(entity.Id));
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
    public async Task RemoveDataUntracked()
    {
        var entity = new TestEntity
        {
            Property = "prop"
        };
        await using var database = await instance.Build();
        await database.AddDataUntracked(entity);
        await database.RemoveDataUntracked(entity);
        Assert.False(await database.Exists(entity.Id));
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

    [Fact]
    public async Task AnyMissing()
    {
        await using var database = await instance.Build();
        await Verify(database.Any<TestEntity>(entity => entity.Id == 10));
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
        await database.AddData(
            new List<object>
            {
                entity1,
                entity2
            },
            entity3);
        Assert.True(await database.Exists(entity1.Id));
        Assert.True(await database.Exists(entity2.Id));
        Assert.True(await database.Exists(entity3.Id));
        Assert.True(callbackCalled);
    }

    [Fact]
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
        Assert.False(await database.Exists(entity1.Id));
        Assert.False(await database.Exists(entity2.Id));
        Assert.False(await database.Exists(entity3.Id));
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
        Assert.True(await database.Exists(entity1.Id));
        Assert.True(await database.Exists(entity2.Id));
        Assert.True(callbackCalled);
    }

    [Fact]
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
        Assert.False(await database.Exists(entity1.Id));
        Assert.False(await database.Exists(entity2.Id));
        Assert.True(callbackCalled);
    }

    [Fact]
    public async Task SuffixedContext()
    {
        var instance = new SqlInstance<TestDbContext>(
            builder => new(builder.Options),
            storage: Storage.FromSuffix<TestDbContext>("theSuffix"));

        var entity = new TestEntity
        {
            Property = "prop"
        };
        await using var database = await instance.Build(new List<object>
        {
            entity
        });
        Assert.True(await database.Exists(entity.Id));
    }

    [Fact]
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
        await using var database = await instance.Build(new List<object>
        {
            entity
        });
        Assert.True(await database.Exists(entity.Id));
        Assert.True(optionsBuilderCalled);
    }

    [Fact]
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
        await using var database = await instance.Build(new List<object>
        {
            entity
        });
        Assert.True(await database.Exists(entity.Id));
    }

    [Fact]
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
        Assert.Equal(dateTime, File.GetCreationTime(instance.Wrapper.DataFile));
    }

    [Fact]
    public async Task Assembly_TimeStamp()
    {
        var instance = new SqlInstance<TestDbContext>(
            builder => new(builder.Options),
            storage: Storage.FromSuffix<TestDbContext>("Assembly_TimeStamp"));

        await using var database = await instance.Build();
        Assert.Equal(Timestamp.LastModified<Tests>(), File.GetCreationTime(instance.Wrapper.DataFile));
    }

    [Fact]
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
        Assert.Equal(Timestamp.LastModified<Tests>(), File.GetCreationTime(instance.Wrapper.DataFile));
    }

    [Fact]
    public async Task WithRebuildDbContext()
    {
        var dateTime = DateTime.Now;
        var instance1 = new SqlInstance<WithRebuildDbContext>(
            builder => new(builder.Options),
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
            builder => new(builder.Options),
            _ => throw new(),
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
        await using var database = await instance.Build(
            new List<object>
            {
                entity
            });
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
        await using var context = await instance.BuildContext(
            new List<object>
            {
                entity
            });
        Assert.NotNull(await context.TestEntities.FindAsync(entity.Id));
        Assert.True(callbackCalled);
    }

    static Tests() =>
        instance = new(
            builder => new(builder.Options),
            callback: (_, _) =>
            {
                callbackCalled = true;
                return Task.CompletedTask;
            });
}
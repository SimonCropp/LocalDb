[TestFixture]
public class Tests :
    LocalDbTestBase<TheDbContext>
{
    [Test]
    public async Task Simple()
    {
        ArrangeData.Companies.Add(
            new()
            {
                Id = Guid.NewGuid(),
                Name = "value"
            });
        await ArrangeData.SaveChangesAsync();

        var entity = await ActData.Companies.SingleAsync();
        entity.Name = "value2";
        await ActData.SaveChangesAsync();

        var result = await AssertData.Companies.SingleAsync();
        await Verify(result);
    }

    [Test]
    public async Task StaticInstance()
    {
        Instance.ArrangeData.Companies.Add(
            new()
            {
                Id = Guid.NewGuid(),
                Name = "value"
            });
        await Instance.ArrangeData.SaveChangesAsync();

        var entity = await Instance.ActData.Companies.SingleAsync();
        entity.Name = "value2";
        await Instance.ActData.SaveChangesAsync();

        var result = await Instance.AssertData.Companies.SingleAsync();
        await Verify(result);
    }

    [Test]
    public Task Combinations()
    {
        string[] inputs = ["value1", "value2"];
        return Combination()
            .Verify(Run, inputs);

        async Task<Company> Run(string input)
        {
            ArrangeData.Companies.Add(
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "value"
                });
            await ArrangeData.SaveChangesAsync();

            var entity = await ActData.Companies.SingleAsync();
            entity.Name = input;
            await ActData.SaveChangesAsync();

            return await AssertData.Companies.SingleAsync();
        }
    }

    [Test]
    public Task Name() =>
        Verify(new
        {
            Database.Name,
            Database.Connection.DataSource
        });

    [Test]
    [TestCase("case")]
    public Task NameWithParams(string caseName) =>
        Verify(new
        {
            Database.Name,
            Database.Connection.DataSource
        });

    [Test]
    public Task ThrowForRedundantIgnoreQueryFilters() =>
        ThrowsTask(
                () =>
                {
                    var entities = AssertData.Companies;
                    return entities.IgnoreQueryFilters().SingleAsync();
                })
            .IgnoreStackTrace();

    [Test]
    public async Task IgnoreQueryFiltersAllowedOnArrangeAndAct()
    {
        await ArrangeData.Companies.IgnoreQueryFilters().ToListAsync();
        await ActData.Companies.IgnoreQueryFilters().ToListAsync();
    }

    [Test]
    public async Task ActInAsync()
    {
        ArrangeData.Companies.Add(
            new()
            {
                Id = Guid.NewGuid(),
                Name = "value"
            });
        await ArrangeData.SaveChangesAsync();

        await AsyncMethod();

        var result = await AssertData.Companies.SingleAsync();
        await Verify(result);

        async Task AsyncMethod()
        {
            await Task.Delay(1);
            var entity = await ActData.Companies.SingleAsync();
            entity.Name = "value2";
            await ActData.SaveChangesAsync();
        }
    }

    [Test]
    public async Task VerifyEntity()
    {
        var entity = new Company
        {
            Id = Guid.NewGuid(),
            Name = "value"
        };
        ArrangeData.Companies.Add(entity);
        await ArrangeData.SaveChangesAsync();
        await VerifyEntity<Company>(entity.Id);
    }

    [Test]
    public async Task VerifyEntities_DbSet()
    {
        ArrangeData.Companies.Add(
            new()
            {
                Id = Guid.NewGuid(),
                Name = "value"
            });
        await ArrangeData.SaveChangesAsync();
        await VerifyEntities(AssertData.Companies);
    }

    [Test]
    public async Task VerifyEntities_Queryable()
    {
        var entity = new Company
        {
            Id = Guid.NewGuid(),
            Name = "value"
        };
        ArrangeData.Companies.Add(entity);
        await ArrangeData.SaveChangesAsync();
        await VerifyEntities(AssertData.Companies.Where(_ => _.Id == entity.Id));
    }

    [Test]
    public async Task VerifyEntity_Queryable()
    {
        var entity = new Company
        {
            Id = Guid.NewGuid(),
            Name = "value"
        };
        ArrangeData.Companies.Add(entity);
        await ArrangeData.SaveChangesAsync();
        await VerifyEntity(AssertData.Companies.Where(_ => _.Id == entity.Id));
    }

    [Test]
    public async Task ArrangeQueryableAfterAct()
    {
        var entity = new Company
        {
            Id = Guid.NewGuid(),
            Name = "value"
        };
        ArrangeData.Companies.Add(entity);
        await ArrangeData.SaveChangesAsync();
        var queryable = ArrangeData.Companies.Where(_ => _.Id == entity.Id);
        // ReSharper disable once UnusedVariable
        var act = ActData;
        await ThrowsTask(() => VerifyEntities(queryable))
            .IgnoreStackTrace()
            .DisableRequireUniquePrefix();
    }

    [Test]
    public Task AccessActAfterAssert()
    {
        // ReSharper disable once UnusedVariable
        var assert = AssertData;
        return Throws(() => ActData)
            .IgnoreStackTrace();
    }

    [Test]
    public async Task ActQueryableAfterAssert()
    {
        var entity = new Company
        {
            Id = Guid.NewGuid(),
            Name = "value"
        };
        ArrangeData.Companies.Add(entity);
        await ArrangeData.SaveChangesAsync();
        var queryable = ActData.Companies.Where(_ => _.Id == entity.Id);
        // ReSharper disable once UnusedVariable
        var assert = AssertData;
        await ThrowsTask(() => VerifyEntities(queryable))
            .IgnoreStackTrace()
            .DisableRequireUniquePrefix();
    }

    [Test]
    public async Task ArrangeQueryableAfterAssert()
    {
        var entity = new Company
        {
            Id = Guid.NewGuid(),
            Name = "value"
        };
        ArrangeData.Companies.Add(entity);
        await ArrangeData.SaveChangesAsync();
        var queryable = ArrangeData.Companies.Where(_ => _.Id == entity.Id);
        // ReSharper disable once UnusedVariable
        var assert = AssertData;
        await ThrowsTask(() => VerifyEntities(queryable))
            .IgnoreStackTrace()
            .DisableRequireUniquePrefix();
    }

    [Test]
    public Task AccessArrangeAfterAssert()
    {
        // ReSharper disable once UnusedVariable
        var assert = AssertData;
        return Throws(() => ArrangeData)
            .IgnoreStackTrace();
    }

    [Test]
    public Task AccessArrangeAfterAct()
    {
        // ReSharper disable once UnusedVariable
        var act = ActData;
        return Throws(() => ArrangeData)
            .IgnoreStackTrace();
    }
}
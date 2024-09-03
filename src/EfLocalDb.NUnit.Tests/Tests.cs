[TestFixture]
public class Tests :
    LocalDbTestBase<TheDbContext>
{
    [Test]
    public async Task Simple()
    {
        ArrangeData.TestEntities.Add(
            new()
            {
                Property = "value"
            });
        await ArrangeData.SaveChangesAsync();

        var entity = await ActData.TestEntities.SingleAsync();
        entity.Property = "value2";
        await ActData.SaveChangesAsync();

        var result = await AssertData.TestEntities.SingleAsync();
        await Verify(result);
    }

    [Test]
    public async Task ActInAsync()
    {
        ArrangeData.TestEntities.Add(
            new()
            {
                Property = "value"
            });
        await ArrangeData.SaveChangesAsync();

        await AsyncMethod();

        var result = await AssertData.TestEntities.SingleAsync();
        await Verify(result);

        async Task AsyncMethod()
        {
            await Task.Delay(1);
            var entity = await ActData.TestEntities.SingleAsync();
            entity.Property = "value2";
            await ActData.SaveChangesAsync();
        }
    }

    [Test]
    public async Task VerifyEntity()
    {
        var entity = new TheEntity
        {
            Property = "value"
        };
        ArrangeData.TestEntities.Add(entity);
        await ArrangeData.SaveChangesAsync();
        await VerifyEntity<TheEntity>(entity.Id);
    }

    [Test]
    public async Task VerifyEntities_DbSet()
    {
        ArrangeData.TestEntities.Add(
            new()
            {
                Property = "value"
            });
        await ArrangeData.SaveChangesAsync();
        await VerifyEntities(AssertData.TestEntities);
    }

    [Test]
    public async Task VerifyEntities_Queryable()
    {
        var entity = new TheEntity
        {
            Property = "value"
        };
        ArrangeData.TestEntities.Add(entity);
        await ArrangeData.SaveChangesAsync();
        await VerifyEntities(AssertData.TestEntities.Where(_ => _.Id == entity.Id));
    }

    [Test]
    public async Task VerifyEntity_Queryable()
    {
        var entity = new TheEntity
        {
            Property = "value"
        };
        ArrangeData.TestEntities.Add(entity);
        await ArrangeData.SaveChangesAsync();
        await VerifyEntity(AssertData.TestEntities.Where(_ => _.Id == entity.Id));
    }

    [Test]
    public async Task ArrangeQueryableAfterAct()
    {
        var entity = new TheEntity
        {
            Property = "value"
        };
        ArrangeData.TestEntities.Add(entity);
        await ArrangeData.SaveChangesAsync();
        var queryable = ArrangeData.TestEntities.Where(_ => _.Id == entity.Id);
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
        var entity = new TheEntity
        {
            Property = "value"
        };
        ArrangeData.TestEntities.Add(entity);
        await ArrangeData.SaveChangesAsync();
        var queryable = ActData.TestEntities.Where(_ => _.Id == entity.Id);
        // ReSharper disable once UnusedVariable
        var assert = AssertData;
        await ThrowsTask(() => VerifyEntities(queryable))
            .IgnoreStackTrace()
            .DisableRequireUniquePrefix();
    }

    [Test]
    public async Task ArrangeQueryableAfterAssert()
    {
        var entity = new TheEntity
        {
            Property = "value"
        };
        ArrangeData.TestEntities.Add(entity);
        await ArrangeData.SaveChangesAsync();
        var queryable = ArrangeData.TestEntities.Where(_ => _.Id == entity.Id);
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
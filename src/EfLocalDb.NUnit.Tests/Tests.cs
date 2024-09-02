#pragma warning disable CS0612 // Type or member is obsolete

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
    public async Task VerifyEntity()
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

        var entity = await ActData.TestEntities.SingleAsync();
        entity.Property = "value2";
        await ActData.SaveChangesAsync();

        await VerifyEntities(AssertData.TestEntities);
    }

    [Test]
    public async Task VerifyEntities_Queryable()
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

        await VerifyEntities(AssertData.TestEntities.Where(_ => _.Id == entity.Id));
    }

    [Test]
    public Task AccessActAfterAssert()
    {
        // ReSharper disable once UnusedVariable
        var assert = AssertData;
        return Throws(() => ActData);
    }

    [Test]
    public Task AccessArrangeAfterAssert()
    {
        // ReSharper disable once UnusedVariable
        var assert = AssertData;
        return Throws(() => ArrangeData);
    }
}
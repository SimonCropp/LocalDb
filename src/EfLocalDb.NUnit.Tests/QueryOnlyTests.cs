// begin-snippet: QueryOnlyTests
[TestFixture]
public class QueryOnlyTests :
    LocalDbTestBase<TheDbContext>
{
    [Test]
    [QueryOnly]
    public async Task CanReadAndWrite()
    {
        ArrangeData.Companies.Add(
            new()
            {
                Id = Guid.NewGuid(),
                Name = "QueryOnly Company"
            });
        await ArrangeData.SaveChangesAsync();

        var entity = await ActData.Companies.SingleAsync();
        AreEqual("QueryOnly Company", entity.Name);
    }

    [Test]
    [QueryOnly]
    public async Task DataIsRolledBack()
    {
        ArrangeData.Companies.Add(
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Should Not Persist"
            });
        await ArrangeData.SaveChangesAsync();

        var count = await ActData.Companies.CountAsync();
        AreEqual(1, count);
    }

    [Test]
    [QueryOnly]
    public async Task StartsWithEmptyDatabase()
    {
        var count = await ActData.Companies.CountAsync();
        AreEqual(0, count);
    }
}
// end-snippet

// begin-snippet: DbQueryTests
[TestFixture]
public class DbQueryTests :
    LocalDbTestBase<TheDbContext>
{
    [Test]
    [DbQuery]
    public async Task CanReadAndWrite()
    {
        ArrangeData.Companies.Add(
            new()
            {
                Id = Guid.NewGuid(),
                Name = "DbQuery Company"
            });
        await ArrangeData.SaveChangesAsync();

        var entity = await ActData.Companies.SingleAsync();
        AreEqual("DbQuery Company", entity.Name);
    }

    [Test]
    [DbQuery]
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
    [DbQuery]
    public async Task StartsWithEmptyDatabase()
    {
        var count = await ActData.Companies.CountAsync();
        AreEqual(0, count);
    }
}
// end-snippet

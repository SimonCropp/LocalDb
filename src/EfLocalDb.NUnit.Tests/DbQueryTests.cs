// begin-snippet: DbQueryTests
[TestFixture]
public class DbQueryTests :
    LocalDbTestBase<TheDbContext>
{
    [Test]
    [DbQuery]
    public async Task ReadFromSharedDb()
    {
        var count = await ActData.Companies.CountAsync();
        AreEqual(0, count);
    }

    [Test]
    [DbQueryWithTransaction]
    public async Task CanReadAndWrite()
    {
        ArrangeData.Companies.Add(
            new()
            {
                Id = Guid.NewGuid(),
                Name = "DbQueryWithTransaction Company"
            });
        await ArrangeData.SaveChangesAsync();

        var entity = await ActData.Companies.SingleAsync();
        AreEqual("DbQueryWithTransaction Company", entity.Name);
    }

    [Test]
    [DbQueryWithTransaction]
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
    [DbQueryWithTransaction]
    public async Task StartsWithEmptyDatabase()
    {
        var count = await ActData.Companies.CountAsync();
        AreEqual(0, count);
    }
}
// end-snippet

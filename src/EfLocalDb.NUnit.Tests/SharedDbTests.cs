// begin-snippet: SharedDbTestsNUnit
[TestFixture]
public class SharedDbTests :
    LocalDbTestBase<TheDbContext>
{
    [Test]
    [SharedDb]
    public async Task ReadFromSharedDb()
    {
        var count = await ActData.Companies.CountAsync();
        AreEqual(0, count);
    }

    [Test]
    [SharedDbWithTransaction]
    public async Task CanReadAndWrite()
    {
        ArrangeData.Companies.Add(
            new()
            {
                Id = Guid.NewGuid(),
                Name = "SharedDbWithTransaction Company"
            });
        await ArrangeData.SaveChangesAsync();

        var entity = await ActData.Companies.SingleAsync();
        AreEqual("SharedDbWithTransaction Company", entity.Name);
    }

    [Test]
    [SharedDbWithTransaction]
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
    [SharedDbWithTransaction]
    public async Task StartsWithEmptyDatabase()
    {
        var count = await ActData.Companies.CountAsync();
        AreEqual(0, count);
    }
}
// end-snippet

// begin-snippet: PooledDbTestsNUnit
[TestFixture]
public class PooledDbTests :
    LocalDbTestBase<TheDbContext>
{
    [Test]
    [PooledDb]
    public async Task StartsFromTemplateState()
    {
        var count = await ActData.Companies.CountAsync();
        AreEqual(0, count);
    }

    [Test]
    [PooledDb]
    public async Task CanReadAndWrite()
    {
        ArrangeData.Companies.Add(
            new()
            {
                Id = Guid.NewGuid(),
                Name = "PooledDb Company"
            });
        await ArrangeData.SaveChangesAsync();

        var entity = await ActData.Companies.SingleAsync();
        AreEqual("PooledDb Company", entity.Name);
    }

    // The next four run concurrently and each writes a row. If the
    // transaction were not rolled back on release, or two tests shared
    // a lease, they would see each other's rows and the count would
    // exceed one.
    [Test]
    [PooledDb]
    public Task IsolatedA() => AssertOnlyOwnRowVisible("A");

    [Test]
    [PooledDb]
    public Task IsolatedB() => AssertOnlyOwnRowVisible("B");

    [Test]
    [PooledDb]
    public Task IsolatedC() => AssertOnlyOwnRowVisible("C");

    [Test]
    [PooledDb]
    public Task IsolatedD() => AssertOnlyOwnRowVisible("D");

    async Task AssertOnlyOwnRowVisible(string name)
    {
        ArrangeData.Companies.Add(
            new()
            {
                Id = Guid.NewGuid(),
                Name = name
            });
        await ArrangeData.SaveChangesAsync();

        var companies = await ActData.Companies.ToListAsync();
        AreEqual(1, companies.Count);
        AreEqual(name, companies[0].Name);
    }
}
// end-snippet

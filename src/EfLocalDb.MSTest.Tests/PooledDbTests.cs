// begin-snippet: PooledDbTestsMSTest
[TestClass]
public class PooledDbTests : LocalDbTestBase<TheDbContext>
{
    [TestMethod]
    [PooledDb]
    public async Task StartsFromTemplateState()
    {
        var count = await ActData.Companies.CountAsync();
        Assert.AreEqual(0, count);
    }

    [TestMethod]
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
        Assert.AreEqual("PooledDb Company", entity.Name);
    }

    // The next four run concurrently and each writes a row. If the
    // transaction were not rolled back on release, or two tests shared
    // a lease, they would see each other's rows and the count would
    // exceed one.
    [TestMethod]
    [PooledDb]
    public Task IsolatedA() => AssertOnlyOwnRowVisible("A");

    [TestMethod]
    [PooledDb]
    public Task IsolatedB() => AssertOnlyOwnRowVisible("B");

    [TestMethod]
    [PooledDb]
    public Task IsolatedC() => AssertOnlyOwnRowVisible("C");

    [TestMethod]
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
        Assert.AreEqual(1, companies.Count);
        Assert.AreEqual(name, companies[0].Name);
    }
}
// end-snippet

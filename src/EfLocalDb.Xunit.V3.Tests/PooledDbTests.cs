// begin-snippet: PooledDbTestsXunitV3
public class PooledDbTests : LocalDbTestBase<TheDbContext>
{
    [Fact]
    [PooledDb]
    public async Task StartsFromTemplateState()
    {
        var count = await ActData.Companies.CountAsync();
        Assert.Equal(0, count);
    }

    [Fact]
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
        Assert.Equal("PooledDb Company", entity.Name);
    }

    // The next four run concurrently and each writes a row. If the
    // transaction were not rolled back on release, or two tests shared
    // a lease, they would see each other's rows and the count would
    // exceed one.
    [Fact]
    [PooledDb]
    public Task IsolatedA() => AssertOnlyOwnRowVisible("A");

    [Fact]
    [PooledDb]
    public Task IsolatedB() => AssertOnlyOwnRowVisible("B");

    [Fact]
    [PooledDb]
    public Task IsolatedC() => AssertOnlyOwnRowVisible("C");

    [Fact]
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
        var only = Assert.Single(companies);
        Assert.Equal(name, only.Name);
    }
}
// end-snippet

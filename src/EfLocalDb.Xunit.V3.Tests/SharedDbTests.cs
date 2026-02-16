// begin-snippet: SharedDbTestsXunitV3
public class SharedDbTests : LocalDbTestBase<TheDbContext>
{
    [Fact]
    [SharedDb]
    public async Task ReadFromSharedDb()
    {
        var count = await ActData.Companies.CountAsync();
        Assert.Equal(0, count);
    }

    [Fact]
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
        Assert.Equal("SharedDbWithTransaction Company", entity.Name);
    }

    [Fact]
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
        Assert.Equal(1, count);
    }

    [Fact]
    [SharedDbWithTransaction]
    public async Task StartsWithEmptyDatabase()
    {
        var count = await ActData.Companies.CountAsync();
        Assert.Equal(0, count);
    }
}
// end-snippet

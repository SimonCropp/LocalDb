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
}
// end-snippet

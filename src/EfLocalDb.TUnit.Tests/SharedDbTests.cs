// begin-snippet: SharedDbTestsTUnit
public class SharedDbTests : LocalDbTestBase<TheDbContext>
{
    [Test]
    [SharedDb]
    public async Task ReadFromSharedDb()
    {
        var count = await ActData.Companies.CountAsync();
        await Assert.That(count).IsEqualTo(0);
    }
}
// end-snippet

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
}
// end-snippet

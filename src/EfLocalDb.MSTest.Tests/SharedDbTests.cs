// begin-snippet: SharedDbTestsMSTest
[TestClass]
public class SharedDbTests : LocalDbTestBase<TheDbContext>
{
    [TestMethod]
    [SharedDb]
    public async Task ReadFromSharedDb()
    {
        var count = await ActData.Companies.CountAsync();
        Assert.AreEqual(0, count);
    }
}
// end-snippet

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

    [TestMethod]
    [SharedDbWithTransaction]
    public async Task CanReadAndWrite()
    {
        ArrangeData.Companies.Add(new() { Id = Guid.NewGuid(), Name = "SharedDbWithTransaction Company" });
        await ArrangeData.SaveChangesAsync();
        var entity = await ActData.Companies.SingleAsync();
        Assert.AreEqual("SharedDbWithTransaction Company", entity.Name);
    }

    [TestMethod]
    [SharedDbWithTransaction]
    public async Task DataIsRolledBack()
    {
        ArrangeData.Companies.Add(new() { Id = Guid.NewGuid(), Name = "Should Not Persist" });
        await ArrangeData.SaveChangesAsync();
        var count = await ActData.Companies.CountAsync();
        Assert.AreEqual(1, count);
    }

    [TestMethod]
    [SharedDbWithTransaction]
    public async Task StartsWithEmptyDatabase()
    {
        var count = await ActData.Companies.CountAsync();
        Assert.AreEqual(0, count);
    }
}

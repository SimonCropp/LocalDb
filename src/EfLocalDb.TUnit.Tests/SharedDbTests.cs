public class SharedDbTests : LocalDbTestBase<TheDbContext>
{
    [Test]
    [SharedDb]
    public async Task ReadFromSharedDb()
    {
        var count = await ActData.Companies.CountAsync();
        await Assert.That(count).IsEqualTo(0);
    }

    [Test]
    [SharedDbWithTransaction]
    public async Task CanReadAndWrite()
    {
        ArrangeData.Companies.Add(new() { Id = Guid.NewGuid(), Name = "SharedDbWithTransaction Company" });
        await ArrangeData.SaveChangesAsync();
        var entity = await ActData.Companies.SingleAsync();
        await Assert.That(entity.Name).IsEqualTo("SharedDbWithTransaction Company");
    }

    [Test]
    [SharedDbWithTransaction]
    public async Task DataIsRolledBack()
    {
        ArrangeData.Companies.Add(new() { Id = Guid.NewGuid(), Name = "Should Not Persist" });
        await ArrangeData.SaveChangesAsync();
        var count = await ActData.Companies.CountAsync();
        await Assert.That(count).IsEqualTo(1);
    }

    [Test]
    [SharedDbWithTransaction]
    public async Task StartsWithEmptyDatabase()
    {
        var count = await ActData.Companies.CountAsync();
        await Assert.That(count).IsEqualTo(0);
    }
}

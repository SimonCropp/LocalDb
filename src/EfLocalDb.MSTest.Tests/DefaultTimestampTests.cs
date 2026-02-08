[TestClass]
public class DefaultTimestampTests :
    LocalDbTestBase<DefaultTimestampDbContext>
{
    static DefaultTimestampTests() =>
        Initialize(
            buildTemplate: async data =>
            {
                await data.Database.EnsureCreatedAsync();
                data.Companies.Add(
                    new()
                    {
                        Id = Guid.NewGuid(),
                        Name = "Default Template Company"
                    });
                await data.SaveChangesAsync();
            });
    // Note: No explicit timestamp provided - should use default behavior

    [TestMethod]
    public async Task NoExplicitTimestamp_UsesDefaultBehavior()
    {
        // Template should have been built with default timestamp behavior
        var company = await AssertData.Companies.SingleAsync();
        await Verify(company);
    }

    [TestMethod]
    public async Task NoExplicitTimestamp_TemplateDataPersists()
    {
        // The template company from Initialize should exist
        var count = await AssertData.Companies.CountAsync();
        Assert.AreEqual(1, count);
    }
}

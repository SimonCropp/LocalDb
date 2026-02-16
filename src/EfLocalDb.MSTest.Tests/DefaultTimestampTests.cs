[TestClass]
public class DefaultTimestampTests : LocalDbTestBase<DefaultTimestampDbContext>
{
    static DefaultTimestampTests() =>
        Initialize(
            buildTemplate: async data =>
            {
                await data.Database.EnsureCreatedAsync();
                data.Companies.Add(new() { Id = Guid.NewGuid(), Name = "Default Template Company" });
                await data.SaveChangesAsync();
            });

    [TestMethod]
    public async Task NoExplicitTimestamp_UsesDefaultBehavior()
    {
        var company = await AssertData.Companies.SingleAsync();
        await Verify(company);
    }

    [TestMethod]
    public async Task NoExplicitTimestamp_TemplateDataPersists()
    {
        var count = await AssertData.Companies.CountAsync();
        Assert.AreEqual(1, count);
    }
}

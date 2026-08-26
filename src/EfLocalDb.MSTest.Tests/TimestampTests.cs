[TestClass]
public class TimestampTests : LocalDbTestBase<TimestampDbContext>
{
    static TimestampTests() =>
        Initialize(
            buildTemplate: async data =>
            {
                await data.Database.EnsureCreatedAsync();
                data.Companies.Add(new() { Id = Guid.NewGuid(), Name = "Template Company" });
                await data.SaveChangesAsync();
            },
            timestamp: Timestamp.LastModified<TimestampDbContext>());

    [TestMethod]
    public async Task ExplicitTimestamp_UsesDbContextAssemblyTimestamp()
    {
        var company = await AssertData.Companies.SingleAsync();
        await Verify(company)
            .Snapshot(
                """
                {
                  Id: Guid_1,
                  Name: Template Company
                }
                """);
    }

    [TestMethod]
    public async Task ExplicitTimestamp_TemplateDataPersists()
    {
        var count = await AssertData.Companies.CountAsync();
        Assert.AreEqual(1, count);
    }
}

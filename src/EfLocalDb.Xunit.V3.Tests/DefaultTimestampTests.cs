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

    [Fact]
    public async Task NoExplicitTimestamp_UsesDefaultBehavior()
    {
        var company = await AssertData.Companies.SingleAsync();
        await Verify(company);
    }

    [Fact]
    public async Task NoExplicitTimestamp_TemplateDataPersists()
    {
        var count = await AssertData.Companies.CountAsync();
        Assert.Equal(1, count);
    }
}

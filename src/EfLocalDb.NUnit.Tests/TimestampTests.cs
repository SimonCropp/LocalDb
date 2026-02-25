[TestFixture]
public class TimestampTests :
    LocalDbTestBase<TimestampDbContext>
{
    static TimestampTests() =>
        Initialize(
            buildTemplate: async data =>
            {
                await data.Database.EnsureCreatedAsync();
                data.Companies.Add(
                    new()
                    {
                        Id = Guid.NewGuid(),
                        Name = "Template Company"
                    });
                await data.SaveChangesAsync();
            },
            timestamp: _ => Timestamp.LastModified<TimestampDbContext>());

    [Test]
    public async Task ExplicitTimestamp_UsesDbContextAssemblyTimestamp()
    {
        // Template should have been built with timestamp from TimestampDbContext assembly
        var company = await AssertData.Companies.SingleAsync();
        await Verify(company);
    }

    [Test]
    public async Task ExplicitTimestamp_TemplateDataPersists()
    {
        // The template company from Initialize should exist
        var count = await AssertData.Companies.CountAsync();
        That(count, Is.EqualTo(1));
    }
}

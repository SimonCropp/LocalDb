[TestFixture]
public class PendingChangesTests
{
    [Test]
    public async Task Run()
    {
        using var instance = new SqlInstance<PendingChangesDbContext>(
            constructInstance: builder => new(builder.Options),
            buildTemplate: _ => _.Database.MigrateAsync());

        await ThrowsTask(() => instance.Build());
    }
}

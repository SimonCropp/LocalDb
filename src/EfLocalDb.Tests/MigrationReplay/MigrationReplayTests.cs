[TestFixture]
public class MigrationReplayTests
{
    static SqlInstance<ReplayDbContext> instance = new(
        constructInstance: builder => new(builder.Options),
        // the template is left empty so every test migrates forward from nothing
        buildTemplate: _ => Task.CompletedTask,
        storage: Storage.FromSuffix<ReplayDbContext>("Replay"));

    [Test]
    public async Task Run()
    {
        await using var database = await instance.Build("Run");

        var appliedAtEachStep = new List<IEnumerable<string>>();
        await database.Context.ReplayRecentMigrations(
            count: 2,
            afterEachMigration: async data =>
                appliedAtEachStep.Add(await data.Database.GetAppliedMigrationsAsync()));

        // one entry per migration, each showing everything applied at that point, which is what
        // distinguishes replaying one at a time from migrating to latest in a single hop
        await Verify(appliedAtEachStep);
    }

    [Test]
    public async Task CountExceedingMigrationsReplaysAll()
    {
        await using var database = await instance.Build("CountExceeding");

        var steps = 0;
        await database.Context.ReplayRecentMigrations(
            count: 100,
            afterEachMigration: _ =>
            {
                steps++;
                return Task.CompletedTask;
            });

        var migrations = database.Context.Database.GetMigrations();
        That(steps, Is.EqualTo(migrations.Count()));
        That(await database.Context.Database.GetPendingMigrationsAsync(), Is.Empty);
    }

    [Test]
    public async Task NoCallbackIsAllowed()
    {
        await using var database = await instance.Build("NoCallback");

        await database.Context.ReplayRecentMigrations(count: 2);

        That(await database.Context.Database.GetPendingMigrationsAsync(), Is.Empty);
    }

    [Test]
    public async Task ThrowsForAlreadyMigratedDatabase()
    {
        await using var database = await instance.Build("AlreadyMigrated");
        await database.Context.Database.MigrateAsync();

        var exception = ThrowsAsync<InvalidOperationException>(
            () => database.Context.ReplayRecentMigrations(count: 2));

        await Verify(exception!.Message);
    }

    [Test]
    public async Task ThrowsForZeroCount()
    {
        await using var database = await instance.Build("ZeroCount");

        ThrowsAsync<ArgumentOutOfRangeException>(
            () => database.Context.ReplayRecentMigrations(count: 0));
    }
}

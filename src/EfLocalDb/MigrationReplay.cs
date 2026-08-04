public static class MigrationReplay
{
    /// <summary>
    /// Applies the last <paramref name="count"/> migrations one at a time, running
    /// <paramref name="afterEachMigration" /> after each, to emulate a run of deployments rather
    /// than a single migrate from empty.
    /// </summary>
    /// <remarks>
    /// A migration that is only ever applied to a database built by migrating an empty one is
    /// tested in the wrong conditions. Deployed databases carry state that a deployment applies
    /// after migrating, for example change tracking, views, or grants, and that state can make DDL
    /// that works on an empty database fail. SQL Server refuses to drop a primary key while change
    /// tracking is on, for instance.
    /// <para>
    /// The migrations are applied one at a time, with <paramref name="afterEachMigration" /> in
    /// between, rather than applied as a batch with a single call at the end. That matters: a table
    /// created by a migration inside the window would otherwise never have the state applied to it
    /// before a later migration alters it, which is exactly the case that tends to break.
    /// </para>
    /// <para>
    /// Requires a database with no migrations applied, since it migrates forward from empty. Build
    /// it from an instance whose <c>buildTemplate</c> leaves the template empty.
    /// </para>
    /// </remarks>
    /// <param name="data">A context pointing at a database with no migrations applied.</param>
    /// <param name="count">How many of the most recent migrations to apply one at a time.</param>
    /// <param name="afterEachMigration">
    /// Applies whatever a deployment does after migrating. Run after every migration, including the
    /// one the window starts from.
    /// </param>
    /// <param name="cancel">A <see cref="Cancel" /> to cancel the operation.</param>
    public static async Task ReplayRecentMigrations<TDbContext>(
        this TDbContext data,
        ushort count = 5,
        Func<TDbContext, Task>? afterEachMigration = null,
        Cancel cancel = default)
        where TDbContext : DbContext
    {
        if (count == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Must be greater than zero.");
        }

        var database = data.Database;

        var applied = await database.GetAppliedMigrationsAsync(cancel);
        if (applied.Any())
        {
            throw new InvalidOperationException(
                $"""
                 {nameof(ReplayRecentMigrations)} requires a database with no migrations applied, since it migrates forward from an empty database.
                 Migrating a database that is already up to date would revert migrations by running their Down.
                 Build the database from an instance with an empty template, for example `buildTemplate: _ => Task.CompletedTask`.
                 Currently applied: {string.Join(", ", applied)}
                 """);
        }

        var migrations = database.GetMigrations().ToList();
        if (migrations.Count == 0)
        {
            throw new InvalidOperationException($"The context '{typeof(TDbContext).Name}' has no migrations.");
        }

        var migrator = data.GetInfrastructure()
            .GetRequiredService<IMigrator>();

        var recent = migrations.TakeLast(count).ToList();

        // everything before the window is one hop: those migrations are not under test
        var startOfWindow = migrations.Count - recent.Count;
        if (startOfWindow > 0)
        {
            await migrator.MigrateAsync(migrations[startOfWindow - 1], cancel);
            await AfterMigration();
        }

        foreach (var migration in recent)
        {
            await migrator.MigrateAsync(migration, cancel);
            await AfterMigration();
        }

        Task AfterMigration() => afterEachMigration?.Invoke(data) ?? Task.CompletedTask;
    }
}

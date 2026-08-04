using System.Data;

/// <summary>
/// The case the feature exists for. A migration that is valid against an empty database fails
/// against a deployed one, because deployment state conflicts with the DDL.
/// </summary>
[TestFixture]
public class ChangeTrackingReplayTests
{
    static SqlInstance<TrackedDbContext> instance = new(
        constructInstance: builder => new(builder.Options),
        buildTemplate: _ => Task.CompletedTask,
        storage: Storage.FromSuffix<TrackedDbContext>("Tracked"));

    [Test]
    public async Task DetectsDdlThatDeploymentStateBreaks()
    {
        await using var database = await instance.Build("Detects");

        // The window is the last two migrations: one creates the table, the next swaps its key.
        // Because tracking is re-applied between them, the swap meets a tracked table, exactly as
        // it would on a deployed database. Applying the window in a single hop and tracking once
        // at the end would not catch this, since the table does not exist when the window starts.
        var exception = ThrowsAsync<SqlException>(
            () => database.Context.ReplayRecentMigrations(
                count: 2,
                afterEachMigration: EnableChangeTracking));

        That(exception!.Message, Does.Contain("change tracking is enabled"));
    }

    [Test]
    public async Task PassesWithoutDeploymentState()
    {
        await using var database = await instance.Build("NoState");

        // the same migrations, with nothing applied between them, are perfectly valid. That gap is
        // why the failure above reaches a deployment rather than a test run.
        await database.Context.ReplayRecentMigrations(count: 2);

        That(await database.Context.Database.GetPendingMigrationsAsync(), Is.Empty);
    }

    static async Task EnableChangeTracking(TrackedDbContext data)
    {
        var connection = (SqlConnection) data.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await Execute(
            connection,
            $"""
             if not exists (select 1 from sys.change_tracking_databases where database_id = db_id())
             begin
                 alter database [{connection.Database}] set change_tracking = on (change_retention = 2 days, auto_cleanup = on);
             end
             """);

        // change tracking requires a primary key, so tables without one are skipped
        await Execute(
            connection,
            """
            declare @table sysname;
            declare tables cursor for
                select t.name
                from sys.tables t
                where t.is_ms_shipped = 0 and
                      t.name <> '__EFMigrationsHistory' and
                      exists (select 1
                              from sys.indexes i
                              where i.object_id = t.object_id and
                                    i.is_primary_key = 1) and
                      not exists (select 1
                                  from sys.change_tracking_tables c
                                  where c.object_id = t.object_id);
            open tables;
            fetch next from tables into @table;
            while @@fetch_status = 0
            begin
                exec('alter table [' + @table + '] enable change_tracking with (track_columns_updated = off)');
                fetch next from tables into @table;
            end
            close tables;
            deallocate tables;
            """);
    }

    static async Task Execute(SqlConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }
}

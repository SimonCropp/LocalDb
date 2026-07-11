/// <summary>
/// Measures <c>auto_close</c> on a cloned database.
///
/// LocalDB databases inherit auto_close on from the model database (an Express-edition
/// default). For test databases that means: every time the last connection closes the database
/// is cleanly shut down, and the next connection pays a full database startup. The library
/// opens per-database connections with pooling disabled, so a shared database accessed by
/// consecutive tests hits that close/reopen cycle whenever test lifetimes do not overlap —
/// and every disposed per-test database triggers a pointless shutdown while the suite is
/// still running.
///
/// The workload is open → select → close cycles against a database that already contains a
/// schema. With auto_close on each cycle re-opens the database; with it off only the first
/// open does.
/// </summary>
[MemoryDiagnoser]
[WarmupCount(3)]
[IterationCount(15)]
[GcServer(true)]
[SuppressMessage("Performance", "CA1822:Mark members as static")]
public class AutoCloseBenchmarks
{
    const string InstanceName = "Benchmark";
    const int TableCount = 40;
    const int Cycles = 5;
    SqlInstance? sqlInstance;
    string connectionString = null!;

    [Params(true, false)]
    public bool AutoClose { get; set; }

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        LocalDbApi.StopAndDelete(InstanceName);
        DirectoryFinder.Delete(InstanceName);

        sqlInstance = new(name: InstanceName, buildTemplate: BuildTables);
        await sqlInstance.Wrapper.AwaitStart();

        var database = await sqlInstance.Build("Reopen");
        connectionString = database.ConnectionString;
        await using (var setup = new SqlConnection(connectionString))
        {
            await setup.OpenAsync();
            await Execute(setup, $"alter database [Reopen] set auto_close {(AutoClose ? "on" : "off")};");
        }

        // Release the connection SqlDatabase holds so the open/close cycles below fully control
        // when the last connection to the database closes.
        await database.DisposeAsync();
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        sqlInstance?.Cleanup(ShutdownMode.KillProcess);
        sqlInstance?.Dispose();
        sqlInstance = null;
    }

    [Benchmark]
    public async Task OpenQueryClose()
    {
        for (var i = 0; i < Cycles; i++)
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            await Execute(connection, "select count(*) from dbo.T0;");
        }
    }

    static async Task BuildTables(SqlConnection connection)
    {
        for (var index = 0; index < TableCount; index++)
        {
            await Execute(connection,
                $"""
                 create table dbo.T{index}
                 (
                     Id uniqueidentifier not null constraint PK_T{index} primary key nonclustered default newid(),
                     A nvarchar(200) null,
                     B int null
                 );
                 insert dbo.T{index} (A, B) values (N'x', 1), (N'y', 2), (N'z', 3);
                 """);
        }
    }

    static async Task Execute(SqlConnection connection, string commandText)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = commandText;
        await command.ExecuteNonQueryAsync();
    }
}

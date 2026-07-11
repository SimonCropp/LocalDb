/// <summary>
/// Measures <c>delayed_durability = forced</c> on a cloned database.
///
/// With full durability (the model-database default) every commit waits for its log records to
/// be flushed to disk. Test workloads are thousands of tiny autocommit transactions against
/// throwaway databases, where losing the tail of the log on a crash is irrelevant — so that
/// per-commit flush wait is pure overhead. Forced delayed durability makes commits return as
/// soon as the log record is written to the in-memory log buffer.
///
/// The workload is small autocommit inserts (one commit, and therefore one log flush when
/// durable, per statement). Database-scoped settings persist through detach/copy/attach, so
/// setting this on the template flows to every clone.
/// </summary>
[MemoryDiagnoser]
[WarmupCount(3)]
[IterationCount(15)]
[GcServer(true)]
[SuppressMessage("Performance", "CA1822:Mark members as static")]
public class DelayedDurabilityBenchmarks
{
    const string InstanceName = "Benchmark";
    const int Inserts = 200;
    SqlInstance? sqlInstance;
    SqlConnection? connection;

    [Params(false, true)]
    public bool Forced { get; set; }

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        LocalDbApi.StopAndDelete(InstanceName);
        DirectoryFinder.Delete(InstanceName);

        sqlInstance = new(
            name: InstanceName,
            buildTemplate: _ => Execute(
                _,
                """
                create table dbo.Rows
                (
                    Id int identity primary key,
                    Value nvarchar(100) null
                );
                """));
        await sqlInstance.Wrapper.AwaitStart();

        var database = await sqlInstance.Build("Durability");
        connection = database.Connection;
        await Execute(connection,
            $"alter database [Durability] set delayed_durability = {(Forced ? "forced" : "disabled")};");
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        sqlInstance?.Cleanup(ShutdownMode.KillProcess);
        sqlInstance?.Dispose();
        sqlInstance = null;
    }

    [Benchmark]
    public async Task AutocommitInserts()
    {
        for (var i = 0; i < Inserts; i++)
        {
            await Execute(connection!, $"insert dbo.Rows (Value) values (N'row {i}');");
        }
    }

    static async Task Execute(SqlConnection connection, string commandText)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = commandText;
        await command.ExecuteNonQueryAsync();
    }
}

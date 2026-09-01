/// <summary>
/// Measures <c>page_verify none</c> against the model-database default of <c>checksum</c>.
///
/// Under CHECKSUM, SQL Server computes a checksum over each 8KB page immediately before it is
/// written to disk, and re-verifies it whenever the page is read back. That exists to detect
/// storage corrupting a page underneath the engine. Test databases are rebuilt from a template
/// and deleted at the end of a run, so a corrupt page is worth nothing to detect: the cost is
/// paid on every page write for a signal no one ever reads.
///
/// The workload isolates the page-write path: one set-based insert dirties a few thousand data
/// pages, then an explicit checkpoint forces them all to disk in one go. Rows are fixed-width
/// and near a page eighth, so row count maps predictably onto page count. delayed_durability is
/// already forced by the template, so commits do not wait on the log and the checkpoint is what
/// drives the measured I/O.
///
/// The table is truncated between iterations, so after warmup the file has reached its steady
/// size and no iteration pays for file growth.
/// </summary>
[MemoryDiagnoser]
[WarmupCount(3)]
[IterationCount(20)]
[GcServer(true)]
[SuppressMessage("Performance", "CA1822:Mark members as static")]
public class PageVerifyBenchmarks
{
    const string InstanceName = "Benchmark";
    const string DatabaseName = "PageVerify";

    // char(900) plus the int key puts eight rows on a page, so this dirties ~7500 data pages
    // (~60MB) per iteration - enough page writes for a per-page cost to show up.
    const int Rows = 60000;

    SqlInstance? sqlInstance;
    SqlConnection? connection;

    [Params("checksum", "none")]
    public string PageVerify { get; set; } = "checksum";

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
                    Padding char(900) not null
                );
                """));
        await sqlInstance.Wrapper.AwaitStart();

        var database = await sqlInstance.Build(DatabaseName);
        connection = database.Connection;
        await Execute(connection, $"alter database [{DatabaseName}] set page_verify {PageVerify};");
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        sqlInstance?.Cleanup(ShutdownMode.KillProcess);
        sqlInstance?.Dispose();
        sqlInstance = null;
    }

    [IterationSetup]
    public void IterationSetup() =>
        Execute(connection!,
                """
                truncate table dbo.Rows;
                checkpoint;
                """)
            .GetAwaiter()
            .GetResult();

    [Benchmark]
    public Task InsertAndCheckpoint() =>
        Execute(connection!,
            $"""
             insert dbo.Rows (Padding)
             select top ({Rows}) 'x'
             from sys.all_objects a
             cross join sys.all_objects b;
             checkpoint;
             """);

    static async Task Execute(SqlConnection connection, string commandText)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = commandText;
        await command.ExecuteNonQueryAsync();
    }
}

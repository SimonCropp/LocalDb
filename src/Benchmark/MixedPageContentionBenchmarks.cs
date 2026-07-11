/// <summary>
/// Measures the downside of <c>mixed_page_allocation on</c>: SGAM allocation contention under
/// concurrent small-object allocation in a single database. Sharing mixed extents means
/// concurrent allocators latch the same SGAM page; SQL Server 2016 made off the default for
/// this reason.
///
/// EfLocalDb clones a separate database file per test (each with its own SGAM), so parallel
/// tests never contend here. It is only reachable when many connections allocate concurrently
/// in one shared database — e.g. parallel <c>[SharedDbWithTransaction]</c> tests. This models
/// that worst case: a fixed total number of create+index+insert+drop operations split across an
/// increasing number of concurrent workers. Ideal scaling halves the time as concurrency
/// doubles; contention shows as the on rows failing to scale like the off rows.
/// </summary>
[MemoryDiagnoser]
[WarmupCount(3)]
[IterationCount(10)]
[GcServer(true)]
[SuppressMessage("Performance", "CA1822:Mark members as static")]
public class MixedPageContentionBenchmarks
{
    const string InstanceName = "Benchmark";
    const int TotalOperations = 320;
    SqlInstance? sqlInstance;
    string dbConnectionString = null!;

    [Params(false, true)]
    public bool MixedPageAllocation { get; set; }

    [Params(1, 4, 8, 16)]
    public int Concurrency { get; set; }

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        LocalDbApi.StopAndDelete(InstanceName);
        DirectoryFinder.Delete(InstanceName);

        sqlInstance = new(name: InstanceName, buildTemplate: _ => Task.CompletedTask);
        await sqlInstance.Wrapper.AwaitStart();

        // Place the database files in the instance directory (fresh each case, deleted with the
        // instance) rather than letting CREATE DATABASE fall back to the user profile default.
        var directory = sqlInstance.Wrapper.Directory;
        await using var master = new SqlConnection(sqlInstance.MasterConnectionString);
        await master.OpenAsync();
        await Execute(master,
            $"""
             create database Contention on
             (name = 'Contention', filename = '{directory}\Contention.mdf')
             log on
             (name = 'Contention_log', filename = '{directory}\Contention_log.ldf');
             alter database Contention set mixed_page_allocation {(MixedPageAllocation ? "on" : "off")};
             """);

        dbConnectionString = new SqlConnectionStringBuilder(sqlInstance.MasterConnectionString)
        {
            InitialCatalog = "Contention"
        }.ConnectionString;
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        sqlInstance?.Cleanup(ShutdownMode.KillProcess);
        sqlInstance?.Dispose();
        sqlInstance = null;
    }

    [Benchmark]
    public async Task ConcurrentAllocate()
    {
        var operationsPerWorker = TotalOperations / Concurrency;
        var tasks = new Task[Concurrency];
        for (var worker = 0; worker < Concurrency; worker++)
        {
            var workerId = worker;
            tasks[worker] = Task.Run(async () =>
            {
                await using var connection = new SqlConnection(dbConnectionString);
                await connection.OpenAsync();
                for (var i = 0; i < operationsPerWorker; i++)
                {
                    var table = $"W{workerId}_{i}";
                    await Execute(connection,
                        $"""
                         create table dbo.[{table}]
                         (
                             Id uniqueidentifier not null constraint [PK_{table}] primary key nonclustered default newid(),
                             A nvarchar(100) null,
                             B int null
                         );
                         create index [IX_{table}] on dbo.[{table}] (B);
                         insert dbo.[{table}] (A, B) values (N'x', 1), (N'y', 2), (N'z', 3);
                         drop table dbo.[{table}];
                         """);
                }
            });
        }

        await Task.WhenAll(tasks);
    }

    static async Task Execute(SqlConnection connection, string commandText)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = commandText;
        await command.ExecuteNonQueryAsync();
    }
}

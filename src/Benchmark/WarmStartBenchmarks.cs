/// <summary>
/// Scenario 3: Warm Start - LocalDB instance is already running and template files exist.
/// This is the "happy path" where everything is ready.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RunStrategy.ColdStart, launchCount: 3, warmupCount: 0, iterationCount: 1)]
[GcServer(true)]
[SuppressMessage("Performance", "CA1822:Mark members as static")]
public class WarmStartBenchmarks
{
    const string InstanceName = "BenchWarm";
    SqlInstance? sqlInstance;

    [Params(0, 1, 5, 10, 100)]
    public int DatabaseCount { get; set; }

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        // Create and start instance - leave it running for warm start
        LocalDbApi.StopAndDelete(InstanceName, ShutdownMode.KillProcess);
        DirectoryFinder.Delete(InstanceName);

        var setupInstance = new SqlInstance(name: InstanceName, buildTemplate: CreateTable);
        await setupInstance.Wrapper.AwaitStart();
        setupInstance.Dispose();
        // Instance remains running for warm start benchmark
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        sqlInstance?.Cleanup(ShutdownMode.KillProcess);
        sqlInstance?.Dispose();
        sqlInstance = null;
    }

    [Benchmark]
    public async Task WarmStart()
    {
        sqlInstance = new(name: InstanceName, buildTemplate: CreateTable);
        await sqlInstance.Wrapper.AwaitStart();

        for (var i = 0; i < DatabaseCount; i++)
        {
            await using var db = await sqlInstance.Build($"BenchDb{i}");
        }
    }

    static async Task CreateTable(SqlConnection connection)
    {
        await using var command = connection.CreateCommand();

        command.CommandText =
            """
            create table MyTable (
                Id int identity(1,1) primary key,
                Value int,
                Name nvarchar(200),
                CreatedAt datetime2 default getdate()
            );
            """;
        await command.ExecuteNonQueryAsync();
    }
}

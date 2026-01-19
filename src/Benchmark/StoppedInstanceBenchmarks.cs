using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

/// <summary>
/// Scenario 2: Stopped Instance - LocalDB instance exists but is stopped, template files exist on disk.
/// This simulates LocalDB auto-shutdown behavior.
/// Note: Due to Wrapper behavior (Wrapper.cs:147-151), when an instance exists but is NOT running,
/// the Wrapper deletes the instance and performs a clean start, wiping the directory.
/// This means Scenario 2 effectively behaves like Scenario 1 (cold start).
/// Each launch gets a fresh state via GlobalSetup.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RunStrategy.ColdStart, launchCount: 3, warmupCount: 0, iterationCount: 1)]
[GcServer(true)]
[SuppressMessage("Performance", "CA1822:Mark members as static")]
public class StoppedInstanceBenchmarks
{
    const string InstanceName = "BenchStopped";
    SqlInstance? sqlInstance;

    [Params(0, 1, 5, 10, 100)]
    public int DatabaseCount { get; set; }

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        // Create instance with template first, then stop it
        LocalDbApi.StopAndDelete(InstanceName, ShutdownMode.KillProcess);
        DirectoryFinder.Delete(InstanceName);

        var setupInstance = new SqlInstance(name: InstanceName, buildTemplate: CreateTable);
        await setupInstance.Wrapper.AwaitStart();
        setupInstance.Dispose();

        // Stop the instance (but keep files on disk)
        LocalDbApi.StopInstance(InstanceName, ShutdownMode.KillProcess);
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        sqlInstance?.Cleanup(ShutdownMode.KillProcess);
        sqlInstance?.Dispose();
        sqlInstance = null;
    }

    [Benchmark]
    public async Task StoppedInstance()
    {
        sqlInstance = new SqlInstance(name: InstanceName, buildTemplate: CreateTable);
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

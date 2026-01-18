using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

/// <summary>
/// Scenario 4: Template Rebuild - LocalDB instance is running but timestamp differs,
/// so the template database needs to be rebuilt.
/// This simulates when code changes require template regeneration.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RunStrategy.ColdStart, launchCount: 3, warmupCount: 0, iterationCount: 1)]
[GcServer(true)]
[SuppressMessage("Performance", "CA1822:Mark members as static")]
public class TemplateRebuildBenchmarks
{
    const string InstanceName = "BenchRebuild";
    SqlInstance? sqlInstance;

    [Params(0, 1, 5, 10, 100)]
    public int DatabaseCount { get; set; }

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        // Create and start instance with initial template
        LocalDbApi.StopAndDelete(InstanceName, ShutdownMode.KillProcess);
        DirectoryFinder.Delete(InstanceName);

        // Use a fixed old timestamp for initial setup
        var oldTimestamp = new DateTime(2020, 1, 1);
        var setupInstance = new SqlInstance(
            name: InstanceName,
            buildTemplate: CreateTable,
            timestamp: oldTimestamp);
        await setupInstance.Wrapper.AwaitStart();
        setupInstance.Dispose();
        // Instance remains running, but we'll use a newer timestamp in the benchmark
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        sqlInstance?.Cleanup(ShutdownMode.KillProcess);
        sqlInstance?.Dispose();
        sqlInstance = null;
    }

    [Benchmark]
    public async Task TemplateRebuild()
    {
        // Use a newer timestamp to force template rebuild
        var newTimestamp = new DateTime(2025, 1, 1);
        sqlInstance = new SqlInstance(
            name: InstanceName,
            buildTemplate: CreateTable,
            timestamp: newTimestamp);
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

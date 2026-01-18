using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

/// <summary>
/// Scenario 1: Cold Start - LocalDB instance does not exist and template files do not exist.
/// This measures the full cold start from scratch.
/// Each launch gets a fresh state via GlobalSetup.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RunStrategy.ColdStart, launchCount: 3, warmupCount: 0, iterationCount: 1)]
[GcServer(true)]
[SuppressMessage("Performance", "CA1822:Mark members as static")]
public class ColdStartBenchmarks
{
    const string InstanceName = "BenchColdStart";
    SqlInstance? sqlInstance;

    [Params(0, 1, 5, 10, 100)]
    public int DatabaseCount { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        // Ensure complete cold start: delete instance and directory
        LocalDbApi.StopAndDelete(InstanceName, ShutdownMode.KillProcess);
        DirectoryFinder.Delete(InstanceName);
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        sqlInstance?.Cleanup(ShutdownMode.KillProcess);
        sqlInstance?.Dispose();
        sqlInstance = null;
    }

    [Benchmark]
    public async Task ColdStart()
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

using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using Microsoft.Data.SqlClient;

[MemoryDiagnoser]
[SuppressMessage("Performance", "CA1822:Mark members as static")]
public class LocalDbBenchmarks
{
    static SqlInstance sqlInstance = null!;
    int databaseCounter;

    [GlobalSetup]
#pragma warning disable CA1822
    public void Setup()
#pragma warning restore CA1822
    {
        LocalDbLogging.EnableVerbose();
        LocalDbSettings.ConnectionBuilder((instance, database) =>
            $"Data Source=(LocalDb)\\{instance};Database={database};Pooling=true;Connection Timeout=300");

        // Force clean start to ensure Optimize: true
        LocalDbApi.StopAndDelete("Benchmark");

        sqlInstance = new(
            name: "Benchmark",
            buildTemplate: CreateTable);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        sqlInstance.Cleanup();
        sqlInstance.Dispose();
    }

    [Benchmark]
    public async Task BuildDatabase()
    {
        var dbName = $"BenchDb{Interlocked.Increment(ref databaseCounter)}";
        await using var database = await sqlInstance.Build(dbName);
    }

    [Benchmark]
    public async Task BuildAndInsert()
    {
        var dbName = $"InsertDb{Interlocked.Increment(ref databaseCounter)}";
        await using var database = await sqlInstance.Build(dbName);
        await AddData(database);
    }

    [Benchmark]
    public async Task BuildInsertAndQuery()
    {
        var dbName = $"QueryDb{Interlocked.Increment(ref databaseCounter)}";
        await using var database = await sqlInstance.Build(dbName);
        await AddData(database);
        await GetData(database);
    }

    static async Task CreateTable(SqlConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "create table MyTable (Value int);";
        await command.ExecuteNonQueryAsync();
    }

    static int intData;

    static async Task AddData(SqlConnection connection)
    {
        await using var command = connection.CreateCommand();
        var addData = Interlocked.Increment(ref intData);
        command.CommandText =
            $"""
             insert into MyTable (Value)
             values ({addData});
             """;
        await command.ExecuteNonQueryAsync();
    }

    static async Task<List<int>> GetData(SqlConnection connection)
    {
        var values = new List<int>();
        await using var command = connection.CreateCommand();
        command.CommandText = "select Value from MyTable";
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            values.Add(reader.GetInt32(0));
        }

        return values;
    }
}

[MemoryDiagnoser]
[WarmupCount(10)]
[IterationCount(50)]
[GcServer(true)]
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
        LocalDbSettings.ConnectionBuilder(_ => _.ConnectTimeout = 300);

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
    public async Task BuildSharedNoTransaction()
    {
        await using var database = await sqlInstance.BuildShared(useTransaction: false);
    }

    [Benchmark]
    public async Task BuildSharedWithTransaction()
    {
        await using var database = await sqlInstance.BuildShared(useTransaction: true);
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

        // Create a table with columns that can hold more data
        command.CommandText =
            """
            create table MyTable (
                Id int identity(1,1) primary key,
                Value int,
                Name nvarchar(200),
                Description nvarchar(max),
                Data varbinary(max),
                CreatedAt datetime2 default getdate()
            );
            """;
        await command.ExecuteNonQueryAsync();

        // Populate template with ~20MB of data
        // Each row: ~2KB actual storage (with SQL overhead)
        // Target ~20MB with ~9,000 rows
        const int batchSize = 1000;
        const int totalRows = 9000;
        var random = new Random(42); // Fixed seed for reproducibility

        for (var batch = 0; batch < totalRows / batchSize; batch++)
        {
            var insertCommand = connection.CreateCommand();
            var sql = new StringBuilder();
            sql.AppendLine("insert into MyTable (Value, Name, Description, Data) values");

            for (var i = 0; i < batchSize; i++)
            {
                var rowNum = batch * batchSize + i;
                var name = $"Item_{rowNum:D6}_{GenerateRandomString(random, 150)}";
                var description = GenerateRandomString(random, 400);
                var dataBytes = new byte[500];
                random.NextBytes(dataBytes);
                var dataHex = Convert.ToHexString(dataBytes);

                if (i > 0)
                {
                    sql.Append(',');
                }

                sql.AppendLine($"({rowNum}, N'{name}', N'{description}', 0x{dataHex})");
            }

            insertCommand.CommandText = sql.ToString();
            await insertCommand.ExecuteNonQueryAsync();
            await insertCommand.DisposeAsync();
        }
    }

    static string GenerateRandomString(Random random, int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var result = new char[length];
        for (var i = 0; i < length; i++)
        {
            result[i] = chars[random.Next(chars.Length)];
        }

        return new(result);
    }

    static int intData;

    static async Task AddData(SqlConnection connection)
    {
        await using var command = connection.CreateCommand();
        var addData = Interlocked.Increment(ref intData);
        command.CommandText =
            $"""
             insert into MyTable (Value, Name, Description)
             values ({addData}, N'BenchmarkItem_{addData}', N'Benchmark test data');
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

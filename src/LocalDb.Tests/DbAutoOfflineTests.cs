[TestFixture]
public class DbAutoOfflineTests
{
    [Test]
    public async Task DbAutoOffline_TakesDatabaseOfflineOnDispose()
    {
        #region DbAutoOfflineUsage

        using var instance = new SqlInstance(
            "DbAutoOffline_Offline",
            TestDbBuilder.CreateTable,
            dbAutoOffline: true);

        #endregion

        string dbName;
        {
            await using var database = await instance.Build();
            dbName = database.Name;
            await TestDbBuilder.AddData(database.Connection);
        }

        // After disposal, check that the database is offline
        var masterConnectionString = instance.MasterConnectionString;
        await using var connection = new SqlConnection(masterConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"select state_desc from sys.databases where name = '{dbName}'";
        var state = (string?)await command.ExecuteScalarAsync();

        AreEqual("OFFLINE", state);

        instance.Cleanup();
    }

    [Test]
    public async Task DbAutoOffline_FilesRemainAfterDispose()
    {
        using var instance = new SqlInstance("DbAutoOffline_Files", TestDbBuilder.CreateTable, dbAutoOffline: true);

        string dbName;
        {
            await using var database = await instance.Build();
            dbName = database.Name;
            await TestDbBuilder.AddData(database.Connection);
        }

        // After disposal, check that the files still exist
        var dataFile = Path.Combine(instance.Wrapper.Directory, $"{dbName}.mdf");
        var logFile = Path.Combine(instance.Wrapper.Directory, $"{dbName}_log.ldf");

        True(File.Exists(dataFile), "Data file should still exist after dispose");
        True(File.Exists(logFile), "Log file should still exist after dispose");

        instance.Cleanup();
    }

    [Test]
    public async Task DbAutoOffline_ExplicitFalse_DoesNotTakeOffline()
    {
        using var instance = new SqlInstance("DbAutoOffline_ExplicitFalse", TestDbBuilder.CreateTable, dbAutoOffline: false);

        string dbName;
        {
            await using var database = await instance.Build();
            dbName = database.Name;
            await TestDbBuilder.AddData(database.Connection);
        }

        // After disposal, check that the database is still online
        var masterConnectionString = instance.MasterConnectionString;
        await using var connection = new SqlConnection(masterConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"select state_desc from sys.databases where name = '{dbName}'";
        var state = (string?)await command.ExecuteScalarAsync();

        AreEqual("ONLINE", state);

        instance.Cleanup();
    }

    [Test]
    public async Task DbAutoOffline_CanBringDatabaseBackOnline()
    {
        using var instance = new SqlInstance("DbAutoOffline_Reattach", TestDbBuilder.CreateTable, dbAutoOffline: true);

        string dbName;
        int data;
        {
            await using var database = await instance.Build();
            dbName = database.Name;
            data = await TestDbBuilder.AddData(database.Connection);
        }

        // Database should be offline now
        var masterConnectionString = instance.MasterConnectionString;
        await using var connection = new SqlConnection(masterConnectionString);
        await connection.OpenAsync();

        // Bring database back online
        await using var onlineCommand = connection.CreateCommand();
        onlineCommand.CommandText = $"alter database [{dbName}] set online";
        await onlineCommand.ExecuteNonQueryAsync();

        // Verify database is online
        await using var stateCommand = connection.CreateCommand();
        stateCommand.CommandText = $"select state_desc from sys.databases where name = '{dbName}'";
        var state = (string?)await stateCommand.ExecuteScalarAsync();
        AreEqual("ONLINE", state);

        // Verify data is intact
        var dbConnectionString = $"Data Source=(LocalDb)\\DbAutoOffline_Reattach;Database={dbName};Integrated Security=True;Encrypt=False";
        await using var dbConnection = new SqlConnection(dbConnectionString);
        await dbConnection.OpenAsync();
        var values = await TestDbBuilder.GetData(dbConnection);
        Contains(data, values);

        instance.Cleanup();
    }

    [Test]
    public async Task DbAutoOffline_MultipleDisposals()
    {
        using var instance = new SqlInstance("DbAutoOffline_Multiple", TestDbBuilder.CreateTable, dbAutoOffline: true);

        for (var i = 0; i < 3; i++)
        {
            await using var database = await instance.Build(databaseSuffix: $"db{i}");
            await TestDbBuilder.AddData(database.Connection);
        }

        // Check all databases are offline
        var masterConnectionString = instance.MasterConnectionString;
        await using var connection = new SqlConnection(masterConnectionString);
        await connection.OpenAsync();

        for (var i = 0; i < 3; i++)
        {
            var expectedDbName = $"DbAutoOfflineTests_DbAutoOffline_MultipleDisposals_db{i}";
            await using var command = connection.CreateCommand();
            command.CommandText = $"select state_desc from sys.databases where name = '{expectedDbName}'";
            var state = (string?)await command.ExecuteScalarAsync();
            AreEqual("OFFLINE", state, $"Database {expectedDbName} should be offline");
        }

        instance.Cleanup();
    }

    [Test]
    [Explicit("Long running benchmark test")]
    public async Task DbAutoOffline_MemoryBenchmark()
    {
        const int databaseCount = 5;
        const int rowsPerDatabase = 10000;

        // Test WITHOUT dbAutoOffline - keep databases open to measure online memory
        using var instanceOnline = new SqlInstance(
            "MemBenchOnline",
            CreateLargerTable,
            dbAutoOffline: false);

        var onlineDatabases = new List<SqlDatabase>();
        for (var i = 0; i < databaseCount; i++)
        {
            var database = await instanceOnline.Build(databaseSuffix: $"db{i}");
            await PopulateAndQueryData(database.Connection, rowsPerDatabase);
            onlineDatabases.Add(database);
        }

        var memoryWithOnline = await GetInstanceMemoryUsageMB(instanceOnline.MasterConnectionString);

        // Dispose online databases (they stay online, just connections closed)
        foreach (var db in onlineDatabases)
        {
            await db.DisposeAsync();
        }

        // Test WITH dbAutoOffline (databases go offline on dispose)
        using var instanceOffline = new SqlInstance(
            "MemBenchOffline",
            CreateLargerTable,
            dbAutoOffline: true);

        for (var i = 0; i < databaseCount; i++)
        {
            await using var database = await instanceOffline.Build(databaseSuffix: $"db{i}");
            await PopulateAndQueryData(database.Connection, rowsPerDatabase);
        }

        var memoryWithOffline = await GetInstanceMemoryUsageMB(instanceOffline.MasterConnectionString);

        var savings = memoryWithOnline - memoryWithOffline;
        var savingsPerDb = savings / databaseCount;

        // Report file sizes before cleanup
        var mdfFiles = Directory.GetFiles(instanceOnline.Wrapper.Directory, "*.mdf")
            .Where(f => !f.Contains("template"))
            .ToList();
        if (mdfFiles.Count > 0)
        {
            var avgFileSizeMB = mdfFiles.Average(f => new FileInfo(f).Length) / 1024.0 / 1024.0;
            Console.WriteLine($"Average .mdf file size: {avgFileSizeMB:F2} MB");
        }

        Console.WriteLine($"Databases: {databaseCount}, Rows per DB: {rowsPerDatabase}");
        Console.WriteLine($"Memory with ONLINE databases:  {memoryWithOnline:F2} MB");
        Console.WriteLine($"Memory with OFFLINE databases: {memoryWithOffline:F2} MB");
        Console.WriteLine($"Total savings: {savings:F2} MB");
        Console.WriteLine($"Savings per database: {savingsPerDb:F2} MB");

        // Verify offline uses less memory
        Less(memoryWithOffline, memoryWithOnline, "Offline databases should use less memory");

        instanceOnline.Cleanup();
        instanceOffline.Cleanup();
    }

    static async Task CreateLargerTable(SqlConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE TestData (
                Id INT IDENTITY PRIMARY KEY,
                Value1 NVARCHAR(100),
                Value2 NVARCHAR(100),
                Value3 NVARCHAR(100),
                CreatedAt DATETIME DEFAULT GETDATE()
            );
            """;
        await command.ExecuteNonQueryAsync();
    }

    static async Task PopulateAndQueryData(SqlConnection connection, int rowCount)
    {
        // Insert data in batches
        await using var insertCommand = connection.CreateCommand();
        insertCommand.CommandText = $"""
            SET NOCOUNT ON;
            DECLARE @i INT = 0;
            WHILE @i < {rowCount}
            BEGIN
                INSERT INTO TestData (Value1, Value2, Value3)
                VALUES (
                    REPLICATE('A', 50) + CAST(@i AS NVARCHAR(10)),
                    REPLICATE('B', 50) + CAST(@i AS NVARCHAR(10)),
                    REPLICATE('C', 50) + CAST(@i AS NVARCHAR(10))
                );
                SET @i = @i + 1;
            END;
            """;
        await insertCommand.ExecuteNonQueryAsync();

        // Query all data to ensure pages are loaded into buffer pool
        await using var selectCommand = connection.CreateCommand();
        selectCommand.CommandText = "SELECT * FROM TestData";
        await using var reader = await selectCommand.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            // Read all rows to load pages into memory
        }
    }

    static async Task<double> GetInstanceMemoryUsageMB(string masterConnectionString)
    {
        await using var connection = new SqlConnection(masterConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                SUM(pages_kb) / 1024.0 AS TotalMemoryMB
            FROM sys.dm_os_memory_clerks
            WHERE type IN (
                'MEMORYCLERK_SQLBUFFERPOOL',
                'MEMORYCLERK_SQLQUERYPLAN',
                'MEMORYCLERK_SQLGENERAL'
            )
            """;
        var result = await command.ExecuteScalarAsync();
        return result is DBNull ? 0 : Convert.ToDouble(result);
    }
}

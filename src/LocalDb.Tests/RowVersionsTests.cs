[TestFixture]
public class RowVersionsTests
{
    [Test]
    public async Task NoTables()
    {
        using var instance = new SqlInstance("GetRowVersions_NoTables", _ => Task.CompletedTask);

        await using var database = await instance.Build();
        var result = await RowVersions.Read(database.Connection);

        IsEmpty(result);
        instance.Cleanup();
    }

    [Test]
    public async Task SingleTable()
    {
        using var instance = new SqlInstance("GetRowVersions_SingleTable", CreateTableWithRowVersion);

        await using var database = await instance.Build();
        var connection = database.Connection;

        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        await InsertRow(connection, id1, "Test1");
        await InsertRow(connection, id2, "Test2");

        var result = await RowVersions.Read(connection);

        AreEqual(2, result.Count);
        True(result.ContainsKey(id1));
        True(result.ContainsKey(id2));
        IsTrue(result[id1] > 0);
        IsTrue(result[id2] > 0);
        AreNotEqual(result[id1], result[id2]);

        instance.Cleanup();
    }

    [Test]
    public async Task Usage()
    {
        using var instance = new SqlInstance("GetRowVersionsUsage", CreateTableWithRowVersion);

        await using var database = await instance.Build();

        #region RowVersionsRead

        var sqlConnection = database.Connection;

        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        await using (var command = sqlConnection.CreateCommand())
        {
            command.CommandText = """
                                  INSERT INTO MyTable (Id, Value) VALUES (@Id1, @Value1);
                                  INSERT INTO MyTable (Id, Value) VALUES (@Id2, @Value2);
                                  """;
            command.Parameters.AddWithValue("@Id1", id1);
            command.Parameters.AddWithValue("@Value1", "Test1");
            command.Parameters.AddWithValue("@Id2", id2);
            command.Parameters.AddWithValue("@Value2", "Test2");
            await command.ExecuteNonQueryAsync();
        }

        var result = await RowVersions.Read(sqlConnection);

        AreEqual(2, result.Count);
        True(result.ContainsKey(id1));
        True(result.ContainsKey(id2));
        IsTrue(result[id1] > 0);
        IsTrue(result[id2] > 0);
        AreNotEqual(result[id1], result[id2]);

        #endregion

        instance.Cleanup();
    }

    [Test]
    public async Task MultipleTables()
    {
        using var instance = new SqlInstance("GetRowVersions_MultipleTables", CreateMultipleTables);

        await using var database = await instance.Build();
        var connection = database.Connection;

        var idTable1 = Guid.NewGuid();
        var idTable2 = Guid.NewGuid();

        await InsertIntoTable1(connection, idTable1, "Value1");
        await InsertIntoTable2(connection, idTable2, 42);

        var result = await RowVersions.Read(connection);

        AreEqual(2, result.Count);
        True(result.ContainsKey(idTable1));
        True(result.ContainsKey(idTable2));
        IsTrue(result[idTable1] > 0);
        IsTrue(result[idTable2] > 0);

        instance.Cleanup();
    }

    [Test]
    public async Task IgnoresTablesWithoutId()
    {
        using var instance = new SqlInstance("GetRowVersions_IgnoresTablesWithoutId", CreateTablesWithAndWithoutId);

        await using var database = await instance.Build();
        var connection = database.Connection;

        var id = Guid.NewGuid();
        await InsertRow(connection, id, "Test");
        await InsertIntoTableWithoutId(connection, "NoId");

        var result = await RowVersions.Read(connection);

        AreEqual(1, result.Count);
        True(result.ContainsKey(id));

        instance.Cleanup();
    }

    [Test]
    public async Task IgnoresTablesWithWrongIdType()
    {
        using var instance = new SqlInstance("GetRowVersions_IgnoresTablesWithWrongIdType", CreateTablesWithDifferentIdTypes);

        await using var database = await instance.Build();
        var connection = database.Connection;

        var validId = Guid.NewGuid();
        await InsertRow(connection, validId, "ValidTable");
        await InsertIntoTableWithIntId(connection, 123, "WrongIdType");

        var result = await RowVersions.Read(connection);

        // Should only include the table with UNIQUEIDENTIFIER Id
        AreEqual(1, result.Count);
        True(result.ContainsKey(validId));

        instance.Cleanup();
    }

    [Test]
    public async Task IgnoresTablesWithoutRowVersion()
    {
        using var instance = new SqlInstance("GetRowVersions_IgnoresTablesWithoutRowVersion", CreateTablesWithAndWithoutRowVersion);

        await using var database = await instance.Build();
        var connection = database.Connection;

        var id = Guid.NewGuid();
        await InsertRow(connection, id, "Test");
        await InsertIntoTableWithoutRowVersion(connection, Guid.NewGuid(), "NoRowVersion");

        var result = await RowVersions.Read(connection);

        AreEqual(1, result.Count);
        True(result.ContainsKey(id));

        instance.Cleanup();
    }

    [Test]
    public async Task RowVersionChangesOnUpdate()
    {
        using var instance = new SqlInstance("GetRowVersions_RowVersionChangesOnUpdate", CreateTableWithRowVersion);

        await using var database = await instance.Build();
        var connection = database.Connection;

        var id = Guid.NewGuid();
        await InsertRow(connection, id, "Original");

        var versionBefore = await RowVersions.Read(connection);
        var originalVersion = versionBefore[id];

        await UpdateRow(connection, id, "Updated");

        var versionAfter = await RowVersions.Read(connection);
        var updatedVersion = versionAfter[id];

        AreNotEqual(originalVersion, updatedVersion);
        IsTrue(updatedVersion > originalVersion);

        instance.Cleanup();
    }

    [Test]
    public async Task EmptyTable()
    {
        using var instance = new SqlInstance("GetRowVersions_EmptyTable", CreateTableWithRowVersion);

        await using var database = await instance.Build();
        var result = await RowVersions.Read(database.Connection);

        IsEmpty(result);
        instance.Cleanup();
    }

    [Test]
    public async Task ManyTables_ExceedsStringAggLimit()
    {
        // Create enough tables to exceed the 8000 byte STRING_AGG limit
        // Each table generates ~60-70 chars in the query, so 150 tables should exceed 8000 bytes
        const int tableCount = 150;
        using var instance = new SqlInstance("GetRowVersions_ManyTables", connection => CreateManyTables(connection, tableCount));

        await using var database = await instance.Build();
        var connection = database.Connection;

        // Insert one row in each table
        var expectedIds = new List<Guid>();
        for (var i = 0; i < tableCount; i++)
        {
            var id = Guid.NewGuid();
            expectedIds.Add(id);
            await InsertIntoTableN(connection, i, id);
        }

        // This should not throw "STRING_AGG aggregation result exceeded the limit of 8000 bytes"
        var result = await RowVersions.Read(connection);

        // Verify all rows were retrieved
        AreEqual(tableCount, result.Count);
        foreach (var id in expectedIds)
        {
            True(result.ContainsKey(id), $"Result should contain ID {id}");
            IsTrue(result[id] > 0, $"RowVersion for {id} should be greater than 0");
        }

        instance.Cleanup();
    }

    [Test]
    public async Task RowVersionByteOrderIsCorrect()
    {
        using var instance = new SqlInstance("GetRowVersions_ByteOrder", CreateTableWithRowVersion);

        await using var database = await instance.Build();
        var connection = database.Connection;

        var id = Guid.NewGuid();
        await InsertRow(connection, id, "Test");

        // Read using RowVersions.Read
        var rowVersions = await RowVersions.Read(connection);
        True(rowVersions.ContainsKey(id), "RowVersions should contain the inserted ID");
        var rowVersionFromHelper = rowVersions[id];

        // Read directly from SQL to get the raw ROWVERSION value
        // SQL Server's ROWVERSION is stored as a monotonically increasing bigint
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT RowVersion FROM MyTable WHERE Id = @Id";
        command.Parameters.AddWithValue("@Id", id);

        var rawBytes = (byte[])(await command.ExecuteScalarAsync())!;

        // Convert the same way RowVersions.Read does (reading as big-endian)
        var expectedRowVersion = System.Buffers.Binary.BinaryPrimitives.ReadUInt64BigEndian(rawBytes);

        // Verify they match
        AreEqual(expectedRowVersion, rowVersionFromHelper,
            "RowVersion from RowVersions.Read should match the direct SQL query value");

        // Also verify the value is reasonable (should be a small positive number)
        IsTrue(rowVersionFromHelper > 0, "RowVersion should be greater than 0");
        IsTrue(rowVersionFromHelper < 1_000_000, "RowVersion should be a reasonable value (not byte-swapped garbage)");

        instance.Cleanup();
    }

    static async Task CreateTableWithRowVersion(SqlConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE MyTable (
                Id UNIQUEIDENTIFIER PRIMARY KEY,
                Value NVARCHAR(100),
                RowVersion ROWVERSION NOT NULL
            );
            """;
        await command.ExecuteNonQueryAsync();
    }

    static async Task CreateMultipleTables(SqlConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE Table1 (
                Id UNIQUEIDENTIFIER PRIMARY KEY,
                Value NVARCHAR(100),
                RowVersion ROWVERSION NOT NULL
            );

            CREATE TABLE Table2 (
                Id UNIQUEIDENTIFIER PRIMARY KEY,
                Number INT,
                RowVersion ROWVERSION NOT NULL
            );
            """;
        await command.ExecuteNonQueryAsync();
    }

    static async Task CreateTablesWithAndWithoutId(SqlConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE MyTable (
                Id UNIQUEIDENTIFIER PRIMARY KEY,
                Value NVARCHAR(100),
                RowVersion ROWVERSION NOT NULL
            );

            CREATE TABLE TableWithoutId (
                SomeId INT PRIMARY KEY,
                Value NVARCHAR(100),
                RowVersion ROWVERSION NOT NULL
            );
            """;
        await command.ExecuteNonQueryAsync();
    }

    static async Task CreateTablesWithDifferentIdTypes(SqlConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE MyTable (
                Id UNIQUEIDENTIFIER PRIMARY KEY,
                Value NVARCHAR(100),
                RowVersion ROWVERSION NOT NULL
            );

            CREATE TABLE TableWithIntId (
                Id INT PRIMARY KEY,
                Value NVARCHAR(100),
                RowVersion ROWVERSION NOT NULL
            );
            """;
        await command.ExecuteNonQueryAsync();
    }

    static async Task CreateTablesWithAndWithoutRowVersion(SqlConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE MyTable (
                Id UNIQUEIDENTIFIER PRIMARY KEY,
                Value NVARCHAR(100),
                RowVersion ROWVERSION NOT NULL
            );

            CREATE TABLE TableWithoutRowVersion (
                Id UNIQUEIDENTIFIER PRIMARY KEY,
                Value NVARCHAR(100)
            );
            """;
        await command.ExecuteNonQueryAsync();
    }

    static async Task InsertRow(SqlConnection connection, Guid id, string value)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO MyTable (Id, Value) VALUES (@Id, @Value)";
        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@Value", value);
        await command.ExecuteNonQueryAsync();
    }

    static async Task InsertIntoTable1(SqlConnection connection, Guid id, string value)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO Table1 (Id, Value) VALUES (@Id, @Value)";
        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@Value", value);
        await command.ExecuteNonQueryAsync();
    }

    static async Task InsertIntoTable2(SqlConnection connection, Guid id, int number)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO Table2 (Id, Number) VALUES (@Id, @Number)";
        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@Number", number);
        await command.ExecuteNonQueryAsync();
    }

    static async Task InsertIntoTableWithoutId(SqlConnection connection, string value)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO TableWithoutId (SomeId, Value) VALUES (@SomeId, @Value)";
        command.Parameters.AddWithValue("@SomeId", 1);
        command.Parameters.AddWithValue("@Value", value);
        await command.ExecuteNonQueryAsync();
    }

    static async Task InsertIntoTableWithIntId(SqlConnection connection, int id, string value)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO TableWithIntId (Id, Value) VALUES (@Id, @Value)";
        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@Value", value);
        await command.ExecuteNonQueryAsync();
    }

    static async Task InsertIntoTableWithoutRowVersion(SqlConnection connection, Guid id, string value)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO TableWithoutRowVersion (Id, Value) VALUES (@Id, @Value)";
        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@Value", value);
        await command.ExecuteNonQueryAsync();
    }

    static async Task UpdateRow(SqlConnection connection, Guid id, string newValue)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE MyTable SET Value = @Value WHERE Id = @Id";
        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@Value", newValue);
        await command.ExecuteNonQueryAsync();
    }

    static async Task CreateManyTables(SqlConnection connection, int count)
    {
        await using var command = connection.CreateCommand();
        var sqlBuilder = new StringBuilder();

        for (var i = 0; i < count; i++)
        {
            sqlBuilder.AppendLine($"""
                CREATE TABLE Table{i} (
                    Id UNIQUEIDENTIFIER PRIMARY KEY,
                    Value NVARCHAR(100),
                    RowVersion ROWVERSION NOT NULL
                );
                """);
        }

        command.CommandText = sqlBuilder.ToString();
        await command.ExecuteNonQueryAsync();
    }

    static async Task InsertIntoTableN(SqlConnection connection, int tableNumber, Guid id)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"INSERT INTO Table{tableNumber} (Id, Value) VALUES (@Id, @Value)";
        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@Value", $"Value{tableNumber}");
        await command.ExecuteNonQueryAsync();
    }
}

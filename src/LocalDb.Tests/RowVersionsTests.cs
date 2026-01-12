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
}

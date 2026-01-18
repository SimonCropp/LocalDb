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
    public async Task DbAutoOffline_Default_DoesNotTakeOffline()
    {
        using var instance = new SqlInstance("DbAutoOffline_Default", TestDbBuilder.CreateTable);

        string dbName;
        {
            await using var database = await instance.Build();
            dbName = database.Name;
            await TestDbBuilder.AddData(database.Connection);
        }

        // After disposal, check that the database is still online (default behavior)
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
}

public static class TestDbBuilder
{
    public static async Task CreateTable(SqlConnection connection, Cancel cancel = default)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "create table MyTable (Value int);";
        await command.ExecuteNonQueryAsync(cancel);
    }

    static int intData;

    public static async Task<int> AddData(SqlConnection connection, Cancel cancel = default)
    {
        await using var command = connection.CreateCommand();
        var addData = intData;
        intData++;
        command.CommandText = $"""
            insert into MyTable (Value)
            values ({addData});
            """;
        await command.ExecuteNonQueryAsync(cancel);
        return addData;
    }

    public static async Task<List<int>> GetData(SqlConnection connection, Cancel cancel = default)
    {
        var values = new List<int>();
        await using var command = connection.CreateCommand();
        command.CommandText = "select Value from MyTable";
        await using var reader = await command.ExecuteReaderAsync(cancel);
        while (await reader.ReadAsync(cancel))
        {
            values.Add(reader.GetInt32(0));
        }

        return values;
    }
}
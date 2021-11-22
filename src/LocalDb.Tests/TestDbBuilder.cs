using Microsoft.Data.SqlClient;

public static class TestDbBuilder
{
    public static async Task CreateTable(SqlConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "create table MyTable (Value int);";
        await command.ExecuteNonQueryAsync();
    }

    static int intData = 0;

    public static async Task<int> AddData(SqlConnection connection)
    {
        await using var command = connection.CreateCommand();
        var addData = intData;
        intData++;
        command.CommandText = $@"
insert into MyTable (Value)
values ({addData});";
        await command.ExecuteNonQueryAsync();
        return addData;
    }

    public static async Task<List<int>> GetData(SqlConnection connection)
    {
        List<int> values = new();
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
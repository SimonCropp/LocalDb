using System.Data.Common;

public static class TestDbBuilder
{
    public static async Task CreateTable(DbConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "create table MyTable (Value int);";
        await command.ExecuteNonQueryAsync();
    }

    static int intData;

    public static async Task<int> AddData(DbConnection connection)
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

    public static async Task<List<int>> GetData(DbConnection connection)
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
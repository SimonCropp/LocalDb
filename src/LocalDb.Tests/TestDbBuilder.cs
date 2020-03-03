using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using Xunit;

public class TestDbBuilder
{
    public static async Task CreateTable(DbConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "create table MyTable (Value int);";
        await command.ExecuteNonQueryAsync();
    }

    public static async Task<int> AddData(DbConnection connection)
    {
        var nextInt = Counters.NextInt();
        await using var command = connection.CreateCommand();
        command.CommandText = $@"
insert into MyTable (Value)
values ({nextInt});";
        await command.ExecuteNonQueryAsync();
        return nextInt;
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
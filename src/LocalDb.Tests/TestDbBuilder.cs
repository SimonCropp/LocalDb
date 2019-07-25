using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using XunitLogger;

public class TestDbBuilder
{
    public static async Task CreateTable(SqlConnection connection)
    {
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "create table MyTable (Value int);";
            await command.ExecuteNonQueryAsync();
        }
    }

    public static async Task<int> AddData(SqlConnection connection)
    {
        var nextInt = Counters.NextInt();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = $@"
insert into MyTable (Value)
values ({nextInt});";
            await command.ExecuteNonQueryAsync();
        }

        return nextInt;
    }

    public static async Task<List<int>> GetData(SqlConnection connection)
    {
        var values = new List<int>();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "select Value from MyTable";
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (reader.Read())
                {
                    values.Add(reader.GetInt32(0));
                }
            }
        }

        return values;
    }
}
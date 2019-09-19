using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.SqlClient;

public static class DbPropertyReader
{
    public static DbSettings Read(SqlConnection connection, string name)
    {
        return new DbSettings
        {
            Files = ReadFileSettings(connection, name).ToList()
        };
    }

    static IEnumerable<DbFileSettings> ReadFileSettings(SqlConnection connection, string name)
    {
        using (var command = connection.CreateCommand())
        {
            command.CommandText = $@"
select name, filename
from master.sys.sysaltfiles
where name like '{name}%'";
            var reader = command.ExecuteReader();
            while (reader.Read())
            {
                yield return new DbFileSettings
                {
                    Name =(string) reader["name"],
                    Filename =(string) reader["filename"]
                };
            }
        }
    }
}
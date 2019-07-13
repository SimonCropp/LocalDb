using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

public static class DbPropertyReader
{
    public static DbSettings Read(SqlConnection connection, Guid id)
    {
        return new DbSettings
        {
            Files = ReadFileSettings(connection, id).ToList()
        };
    }

    static IEnumerable<DbFileSettings> ReadFileSettings(SqlConnection connection, Guid id)
    {
        using (var command = connection.CreateCommand())
        {
            command.CommandText = $@"
select name, filename
from master.sys.sysaltfiles
where name like '{id}%'";
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
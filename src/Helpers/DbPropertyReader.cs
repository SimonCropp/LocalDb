﻿using System.Data.Common;

public static class DbPropertyReader
{
    public static DbSettings Read(DbConnection connection, string name)
    {
        return new DbSettings(ReadFileSettings(connection, name).ToList());
    }

    static IEnumerable<DbFileSettings> ReadFileSettings(DbConnection connection, string name)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $@"
select name, filename
from master.sys.sysaltfiles
where name like '{name}%'";
        var reader = command.ExecuteReader();
        while (reader.Read())
        {
            yield return new DbFileSettings(
                name: (string) reader["name"],
                filename: (string) reader["filename"]
            );
        }
    }
}
static class DbFileNameReader
{
    public static async Task<(string? data, string? log)> ReadFileInfo(this SqlConnection connection, string dbName, Cancel cancel = default)
    {
        var datafileName = await connection.ReadFileName(dbName, "ROWS", cancel);
        var logFileName = await connection.ReadFileName(dbName, "LOG", cancel);
        datafileName = Path.GetFileName(datafileName);
        logFileName = Path.GetFileName(logFileName);
        return (datafileName, logFileName);
    }

    static async Task<string?> ReadFileName(this SqlConnection connection, string dbName, string type, Cancel cancel = default)
    {
#if(NET5_0_OR_GREATER)
        await using var command = connection.CreateCommand();
#else
        using var command = connection.CreateCommand();
#endif
        command.CommandText = $"""
            select
            d.name,
            f.physical_name,
            f.type_desc
            from sys.master_files f
            inner join sys.databases d on d.database_id = f.database_id
            where d.name = N'{dbName}' and f.type_desc = N'{type}'
            """;
#if(NET5_0_OR_GREATER)
        await using var reader = await command.ExecuteReaderAsync(cancel);
#else
        using var reader = await command.ExecuteReaderAsync(cancel);
#endif
        while (await reader.ReadAsync(cancel))
        {
            return (string) reader["physical_name"];
        }

        return null;
    }
}
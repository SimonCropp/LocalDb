using System.Data.Common;

static class DbFileNameReader
{
    public static async Task<(string? data, string? log)> ReadFileInfo(this DbConnection connection, string dbName)
    {
        var datafileName = await connection.ReadFileName(dbName, "ROWS");
        var logFileName = await connection.ReadFileName(dbName, "LOG");
        datafileName = Path.GetFileName(datafileName);
        logFileName = Path.GetFileName(logFileName);
        return (datafileName, logFileName);
    }

    static async Task<string?> ReadFileName(this DbConnection connection, string dbName, string type)
    {
#if(NET5_0)
        await using var command = connection.CreateCommand();
#else
        using var command = connection.CreateCommand();
#endif
        command.CommandText = $@"
select
d.name,
f.physical_name,
f.type_desc
from sys.master_files f
inner join sys.databases d on d.database_id = f.database_id
where d.name = '{dbName}' and f.type_desc = '{type}'";
#if(NET5_0)
        await using var reader = await command.ExecuteReaderAsync();
#else
        using var reader = await command.ExecuteReaderAsync();
#endif
        while (await reader.ReadAsync())
        {
            return (string) reader["physical_name"];
        }

        return null;
    }
}
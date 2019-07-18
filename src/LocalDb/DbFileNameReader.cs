using System.Data.SqlClient;
using System.IO;

static class DbFileNameReader
{
    public static (string data, string log) ReadFileInfo(this SqlConnection connection, string dbName)
    {
        var datafileName = connection.ReadFileName(dbName, "ROWS");
        var logFileName = connection.ReadFileName(dbName, "LOG");
        if (datafileName != null)
        {
            datafileName = Path.GetFileName(datafileName);
        }

        if (logFileName != null)
        {
            logFileName = Path.GetFileName(logFileName);
        }

        return (datafileName, logFileName);
    }

    static string ReadFileName(this SqlConnection connection, string dbName, string type)
    {
        using (var command = connection.CreateCommand())
        {
            command.CommandText = $@"
select
	d.name,
	f.physical_name,
	f.type_desc
from sys.master_files f
inner join sys.databases d on d.database_id = f.database_id
where d.name = '{dbName}' and f.type_desc = '{type}'";
            var reader = command.ExecuteReader();
            while (reader.Read())
            {
                return (string) reader["physical_name"];
            }
        }

        return null;
    }
}
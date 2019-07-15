using System.Data.SqlClient;
using System.IO;

static class DbFileNameReader
{
    public static (string data, string log) ReadFileInfo(this SqlConnection connection, string dbName)
    {
        var datafileName = connection.ReadFileName(dbName);
        var logFileName = connection.ReadFileName($"{dbName}_log");
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

    static string ReadFileName(this SqlConnection connection, string dbName)
    {
        using (var command = connection.CreateCommand())
        {
            command.CommandText = $@"
select name, filename
from master.sys.sysaltfiles
where name like '{dbName}'";
            var reader = command.ExecuteReader();
            while (reader.Read())
            {
                return (string) reader["filename"];
            }
        }

        return null;
    }
}
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

static class InfoReader
{
    public static async Task<DatabaseState> ReadDatabaseState(this Wrapper wrapper,string dbName)
    {
        var dataFile = Path.Combine(wrapper.Directory, $"{dbName}.mdf");
        var logFile = Path.Combine(wrapper.Directory, $"{dbName}_log.ldf");
        var connection = new SqlConnection(wrapper.MasterConnectionString);
        await connection.OpenAsync();
        var dbFileInfo = await connection.ReadFileInfo(dbName);

        return new DatabaseState
        {
            DataFileExists = File.Exists(dataFile),
            LogFileExists = File.Exists(logFile),
            DbDataFileName = dbFileInfo.data,
            DbLogFileName = dbFileInfo.log,
        };
    }
}
using Microsoft.Data.SqlClient;

static class InfoReader
{
    public static async Task<DatabaseState> ReadDatabaseState(this Wrapper wrapper, string dbName)
    {
        var dataFile = Path.Combine(wrapper.Directory, $"{dbName}.mdf");
        var logFile = Path.Combine(wrapper.Directory, $"{dbName}_log.ldf");
        await using var connection = new SqlConnection(wrapper.MasterConnectionString);
        await connection.OpenAsync();
        var dbFileInfo = await connection.ReadFileInfo(dbName);
        return new(
            dataFileExists: File.Exists(dataFile),
            logFileExists: File.Exists(logFile),
            dbDataFileName: dbFileInfo.data,
            dbLogFileName: dbFileInfo.log
        );
    }
}
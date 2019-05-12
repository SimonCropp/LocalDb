using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;

public class LocalDbWrapper
{
    string dataDirectory;
    public readonly string MasterConnection;
    string key;

    public LocalDbWrapper(string key, string dataDirectory)
    {
        this.key = key;
        MasterConnection = $"Data Source=(LocalDb)\\{key};Database=master; Integrated Security=True";
        this.dataDirectory = dataDirectory;
        Directory.CreateDirectory(dataDirectory);
    }

    public void Detach(string dbName)
    {
        using (var connection = new SqlConnection(MasterConnection))
        using (var command = connection.CreateCommand())
        {
            connection.Open();
            command.CommandText = $"EXEC sp_detach_db '{dbName}', 'true';";
            command.ExecuteNonQuery();
        }
    }

    public async Task<string> CreateDatabaseFromTemplate(string dbName, string templateDbName)
    {
        var dbFilePath = Path.Combine(dataDirectory, $"{dbName}.mdf");
        var dbLogPath = Path.Combine(dataDirectory, $"{dbName}.ldf");
        var templateDataFile = Path.Combine(dataDirectory, templateDbName + ".mdf");
        var templateLogFile = Path.Combine(dataDirectory, templateDbName + ".ldf");

        File.Copy(templateDataFile, dbFilePath);
        File.Copy(templateLogFile, dbLogPath);

        using (var connection = new SqlConnection(MasterConnection))
        using (var command = connection.CreateCommand())
        {
            command.CommandText = $@"
create database [{dbName}] on
(
    name = [{dbName}],
    filename = '{dbFilePath}',
    size = 10MB,
    maxSize = 10GB,
    fileGrowth = 5MB
)
log on
(
    name = [{dbName}_log],
    filename = '{dbLogPath}',
    size = 10MB,
    maxSize = 10GB,
    fileGrowth = 5MB
)
for attach;
";
            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        return $"Data Source=(LocalDb)\\{key};Database={dbName}; Integrated Security=True";
    }

    public void CreateDatabase(string name)
    {
        var dataFile = Path.Combine(dataDirectory, name + ".mdf");
        var logFile = Path.Combine(dataDirectory, name + ".ldf");
        using (var connection = new SqlConnection(MasterConnection))
        using (var command = connection.CreateCommand())
        {
            command.CommandText = $@"
create database [{name}] on
(
    name = [{name}],
    filename = '{dataFile}',
    size = 10MB,
    maxSize = 10GB,
    fileGrowth = 5MB
)
log on
(
    name = [{name}_log],
    filename = '{logFile}',
    size = 10MB,
    maxSize = 10GB,
    fileGrowth = 5MB
);
";
            connection.Open();
            command.ExecuteNonQuery();
        }
    }
}
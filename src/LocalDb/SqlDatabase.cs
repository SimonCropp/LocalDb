using Microsoft.Data.SqlClient;
using DataSqlConnection = System.Data.SqlClient.SqlConnection;

namespace LocalDb;

public class SqlDatabase :
#if(NET5_0)
    IAsyncDisposable,
#endif
    IDisposable
{
    Func<Task> delete;

    internal SqlDatabase(string connectionString, string name, Func<Task> delete)
    {
        this.delete = delete;
        ConnectionString = connectionString;
        Name = name;
        Connection = new SqlConnection(connectionString);
    }

    public string ConnectionString { get; }
    public string Name { get; }

    public SqlConnection Connection { get; }

    public static implicit operator SqlConnection(SqlDatabase instance)
    {
        return instance.Connection;
    }

    public async Task<SqlConnection> OpenNewConnection()
    {
        var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        return connection;
    }

    public async Task<DataSqlConnection> OpenNewDataConnection()
    {
        var connection = new DataSqlConnection(ConnectionString);
        await connection.OpenAsync();
        return connection;
    }

    public async Task Start()
    {
        await Connection.OpenAsync();
    }

    public void Dispose()
    {
        Connection.Dispose();
    }

#if(!NETSTANDARD2_0 && !NET461)
    public ValueTask DisposeAsync()
    {
        return Connection.DisposeAsync();
    }
#endif

    public async Task Delete()
    {
#if(NETSTANDARD2_0 || NET461)
        Dispose();
#else
        await DisposeAsync();
#endif
        await delete();
    }
}
using Microsoft.Data.SqlClient;

namespace LocalDb;

public class SqlDatabase :
    IAsyncDisposable,
    IDisposable
{
    Func<Task> delete;

    internal SqlDatabase(string connectionString, string name, Func<Task> delete)
    {
        this.delete = delete;
        ConnectionString = connectionString;
        Name = name;
        Connection = new(connectionString);
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
        SqlConnection connection = new(ConnectionString);
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

    public ValueTask DisposeAsync()
    {
        return Connection.DisposeAsync();
    }

    public async Task Delete()
    {
        await DisposeAsync();
        await delete();
    }
}
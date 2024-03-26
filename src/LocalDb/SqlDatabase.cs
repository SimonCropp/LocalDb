namespace LocalDb;

public partial class SqlDatabase :
#if(NET5_0_OR_GREATER)
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
        Connection = new(connectionString);
        dataConnection = new(() =>
        {
            var connection = new DataSqlConnection(connectionString);
            connection.Open();
            return connection;
        });
    }

    public string ConnectionString { get; }
    public string Name { get; }

    public SqlConnection Connection { get; }
    Lazy<DataSqlConnection> dataConnection;
    public DataSqlConnection DataConnection => dataConnection.Value;

    public static implicit operator SqlConnection(SqlDatabase instance) => instance.Connection;

    public static implicit operator DbConnection(SqlDatabase instance) => instance.Connection;

    public static implicit operator DataSqlConnection(SqlDatabase instance) => instance.DataConnection;

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

    public Task Start() => Connection.OpenAsync();

    public void Dispose()
    {
        Connection.Dispose();
        if (dataConnection.IsValueCreated)
        {
            dataConnection.Value.Dispose();
        }
    }

#if(!NET48)
    public async ValueTask DisposeAsync()
    {
        await Connection.DisposeAsync();
        if (dataConnection.IsValueCreated)
        {
            await dataConnection.Value.DisposeAsync();
        }
    }
#endif

    // ReSharper disable once ReplaceAsyncWithTaskReturn
    public async Task Delete()
    {
#if(NET48)
        Dispose();
#else
        await DisposeAsync();
#endif
        await delete();
    }
}
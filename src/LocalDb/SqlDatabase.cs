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
    }

    public string ConnectionString { get; }
    public string Name { get; }

    public SqlConnection Connection { get; }

    public static implicit operator SqlConnection(SqlDatabase instance) => instance.Connection;

    public async Task<SqlConnection> OpenNewConnection()
    {
        var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        return connection;
    }

    public Task Start() => Connection.OpenAsync();

    public void Dispose() =>
        Connection.Dispose();

#if(!NET48)
    public ValueTask DisposeAsync() =>
        Connection.DisposeAsync();
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
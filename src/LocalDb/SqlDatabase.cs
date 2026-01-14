namespace LocalDb;

public partial class SqlDatabase :
#if(NET5_0_OR_GREATER)
    IAsyncDisposable,
#endif
    IDisposable
{
    Func<Cancel, Task> delete;

    internal SqlDatabase(SqlConnection connection, string name, Func<Cancel, Task> delete)
    {
        this.delete = delete;
        ConnectionString = connection.ConnectionString;
        Name = name;
        Connection = connection;
    }

    public string ConnectionString { get; }
    public string Name { get; }

    public SqlConnection Connection { get; }

    public static implicit operator SqlConnection(SqlDatabase instance) => instance.Connection;

    public async Task<SqlConnection> OpenNewConnection(Cancel cancel = default)
    {
        var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync(cancel);
        return connection;
    }

    public void Dispose() =>
        Connection.Dispose();

#if(!NET48)
    public ValueTask DisposeAsync() =>
        Connection.DisposeAsync();
#endif

    // ReSharper disable once ReplaceAsyncWithTaskReturn
    public async Task Delete(Cancel cancel = default)
    {
#if(NET48)
        Dispose();
#else
        await DisposeAsync();
#endif
        await delete(cancel);
    }
}

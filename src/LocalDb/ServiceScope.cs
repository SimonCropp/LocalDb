class ServiceScope(DataSqlConnection dataConnection, SqlConnection connection) :
#if(NET5_0_OR_GREATER)
    IAsyncDisposable,
#endif
#if NET7_0_OR_GREATER
    IServiceScope,
#endif
    IServiceProvider
{
    public void Dispose()
    {
        connection.Dispose();
        dataConnection.Dispose();
    }

#if(NET5_0_OR_GREATER)
    public async ValueTask DisposeAsync()
    {
        await connection.DisposeAsync();
        await dataConnection.DisposeAsync();
    }
#endif

    public IServiceProvider ServiceProvider => this;

    public object? GetService(Type type)
    {
        if (type == typeof(DataSqlConnection))
        {
            return dataConnection;
        }

        if (type == typeof(SqlConnection))
        {
            return connection;
        }

        return null;
    }
}
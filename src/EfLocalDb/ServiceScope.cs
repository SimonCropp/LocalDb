class ServiceScope :
    IServiceScope,
    IServiceProvider,
    IAsyncDisposable
{
    DbContext context;
    DataSqlConnection dataConnection;
    SqlConnection connection;

    public ServiceScope(DbContext context, DataSqlConnection dataConnection, SqlConnection connection)
    {
        this.context = context;
        this.dataConnection = dataConnection;
        this.connection = connection;
    }

    public void Dispose()
    {
        connection.Dispose();
        dataConnection.Dispose();
        context.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await connection.DisposeAsync();
        await dataConnection.DisposeAsync();
        await context.DisposeAsync();
    }

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

        if (type == context.GetType())
        {
            return context;
        }

        return null;
    }
}
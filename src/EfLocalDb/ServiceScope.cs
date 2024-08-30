class ServiceScope(DbContext context, SqlConnection connection) :
    IServiceScope,
    IServiceProvider,
    IAsyncDisposable
{
    public void Dispose()
    {
        connection.Dispose();
        context.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await connection.DisposeAsync();
        await context.DisposeAsync();
    }

    public IServiceProvider ServiceProvider => this;

    public object? GetService(Type type)
    {
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
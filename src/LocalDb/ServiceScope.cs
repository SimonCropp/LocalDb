class ServiceScope(SqlConnection connection) :
#if NET5_0_OR_GREATER
    IAsyncDisposable,
#endif
#if NET7_0_OR_GREATER
    IServiceScope,
#endif
    IServiceProvider
{
    public void Dispose() =>
        connection.Dispose();

#if NET5_0_OR_GREATER
    public ValueTask DisposeAsync() =>
        connection.DisposeAsync();
#endif

    public IServiceProvider ServiceProvider => this;

    public object? GetService(Type type)
    {
        if (type == typeof(SqlConnection))
        {
            return connection;
        }

        return null;
    }
}

namespace LocalDb;

public partial class SqlDatabase :
#if NET7_0_OR_GREATER
    IServiceScopeFactory,
#endif
    IServiceProvider
{
    public object? GetService(Type type)
    {
        if (type == typeof(SqlConnection))
        {
            return Connection;
        }

#if NET7_0_OR_GREATER
        if (type == typeof(IServiceScopeFactory))
        {
            return this;
        }
#endif

        return null;
    }

#if NET7_0_OR_GREATER
    public IServiceScope CreateScope()
    {
        var connection = new SqlConnection(ConnectionString);
        connection.Open();
        return new ServiceScope(connection);
    }

    public AsyncServiceScope CreateAsyncScope() =>
        new(CreateScope());
#endif
}
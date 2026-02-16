#if NET7_0_OR_GREATER
class ServiceScope(DbContext context, SqlConnection connection) :
    IServiceScope,
    IServiceProvider
{
    DbContext context = context;
    SqlConnection connection = connection;

    public void Dispose()
    {
        connection.Dispose();
        context.Dispose();
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
#endif

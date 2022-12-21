#if(NET7_0_OR_GREATER)
class ServiceScope :
    IServiceScope,
    IServiceProvider
{
    DbContext context;
    DataSqlConnection connection;

    public ServiceScope(DbContext context, DataSqlConnection connection)
    {
        this.context = context;
        this.connection = connection;
    }

    public void Dispose()
    {
        connection.Dispose();
        context.Dispose();
    }

    public IServiceProvider ServiceProvider => this;

    public object? GetService(Type type)
    {
        if (type == typeof(DataSqlConnection))
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
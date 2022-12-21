namespace EfLocalDb;

public partial class SqlDatabase<TDbContext> :
#if(NET7_0_OR_GREATER)
    IServiceScopeFactory,
#endif
    IServiceProvider
{
    public object? GetService(Type type)
    {
        if (type == typeof(DataSqlConnection))
        {
            return Connection;
        }

        if (type == typeof(TDbContext))
        {
            return Context;
        }

#if(NET7_0_OR_GREATER)
        if (type == typeof(IServiceScopeFactory))
        {
            return this;
        }
#endif

        return null;
    }

#if(NET7_0_OR_GREATER)
    public IServiceScope CreateScope()
    {
        var connection = new DataSqlConnection(ConnectionString);
        connection.Open();
        return new ServiceScope(NewDbContext(), connection);
    }

    public AsyncServiceScope CreateAsyncScope() =>
        new(CreateScope());
#endif
}
namespace EfLocalDb;

public partial class SqlDatabase<TDbContext> :
#if(NET7_0_OR_GREATER)
    IServiceScopeFactory,
#endif
    IServiceProvider
{
    public object? GetService(Type serviceType)
    {
        if (serviceType == typeof(DataSqlConnection))
        {
            return Connection;
        }

        if (serviceType == typeof(TDbContext))
        {
            return Context;
        }

        return null;
    }

#if(NET7_0_OR_GREATER)
    public IServiceScope CreateScope()
    {
        var connection = new DataSqlConnection(ConnectionString);
        connection.Open();
        return new ServiceScope(NewDbContext(), connection);
    }
#endif
}
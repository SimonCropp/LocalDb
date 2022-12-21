namespace EfLocalDb;

public partial class SqlDatabase<TDbContext> :
#if(NET7_0_OR_GREATER)
    IServiceScopeFactory,
#endif
    IServiceProvider
{
    public object? GetService(Type serviceType)
    {
        if (serviceType == typeof(SqlConnection))
        {
            return Connection;
        }

        if (serviceType == typeof(DataSqlConnection))
        {
            return DataConnection;
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
        var connection = new SqlConnection(ConnectionString);
        connection.Open();
        var dataConnection = new DataSqlConnection(ConnectionString);
        dataConnection.Open();
        return new ServiceScope(NewDbContext(), dataConnection, connection);
    }
#endif
}
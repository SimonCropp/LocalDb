namespace EfLocalDb;

public partial class SqlDatabase<TDbContext> :
#if(NET7_0_OR_GREATER)
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

        if (type == typeof(DataSqlConnection))
        {
            return DataConnection;
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
        var connection = new SqlConnection(ConnectionString);
        connection.Open();
        var dataConnection = new DataSqlConnection(ConnectionString);
        dataConnection.Open();
        return new ServiceScope(NewDbContext(), dataConnection, connection);
    }

    public AsyncServiceScope CreateAsyncScope() =>
        new(CreateScope());
#endif
}
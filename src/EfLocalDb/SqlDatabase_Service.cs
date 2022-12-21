namespace EfLocalDb;

public partial class SqlDatabase<TDbContext> :
    IServiceProvider,
    IServiceScopeFactory
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

    public IServiceScope CreateScope()
    {
        var connection = new SqlConnection(ConnectionString);
        connection.Open();
        var dataConnection = new DataSqlConnection(ConnectionString);
        dataConnection.Open();
        return new ServiceScope(NewDbContext(), dataConnection, connection);
    }
}
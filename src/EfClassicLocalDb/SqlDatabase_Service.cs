namespace EfLocalDb;

public partial class SqlDatabase<TDbContext> :
    IServiceProvider,
    IServiceScopeFactory
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

    public IServiceScope CreateScope()
    {
        var connection = new DataSqlConnection(ConnectionString);
        connection.Open();
        return new ServiceScope(NewDbContext(), connection);
    }
}
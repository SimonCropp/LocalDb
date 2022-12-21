namespace EfLocalDb;

public partial class SqlDatabase<TDbContext> :
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
}
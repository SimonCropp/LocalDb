namespace EfLocalDb;

public partial class SqlDatabase<TDbContext> :
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
}
namespace LocalDb;

public partial class SqlDatabase :
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

        return null;
    }
}
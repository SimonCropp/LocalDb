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

    public IServiceScope CreateScope()
    {
        var connection = new SqlConnection(ConnectionString);
        connection.Open();
        var dataConnection = new DataSqlConnection(ConnectionString);
        dataConnection.Open();
        return new ServiceScope(dataConnection, connection);
    }
}
using System;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace LocalDb
{
    public class SqlDatabase :
        IDisposable
    {
        public SqlDatabase(string connectionString)
        {
            Guard.AgainstNullWhiteSpace(nameof(connectionString), connectionString);
            ConnectionString = connectionString;
            Connection = new SqlConnection(connectionString);
        }

        public string ConnectionString { get; }

        public SqlConnection Connection { get; }

        public static implicit operator SqlConnection(SqlDatabase instance)
        {
            Guard.AgainstNull(nameof(instance), instance);
            return instance.Connection;
        }

        public async Task<SqlConnection> OpenNewConnection()
        {
            var sqlConnection = new SqlConnection(ConnectionString);
            await sqlConnection.OpenAsync();
            return sqlConnection;
        }

        public Task Start()
        {
            return Connection.OpenAsync();
        }

        public void Dispose()
        {
            Connection.Dispose();
        }
    }
}
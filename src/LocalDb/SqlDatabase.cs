using System;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace LocalDb
{
    public class SqlDatabase :
        IDisposable
    {
        public string ConnectionString{ get; }

        public SqlDatabase(string connectionString)
        {
            ConnectionString = connectionString;
            Connection = new SqlConnection(connectionString);
        }

        public SqlConnection Connection { get; }

        public async Task<SqlConnection> OpenConnection()
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
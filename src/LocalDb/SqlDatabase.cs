using System;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace LocalDb
{
    public class SqlDatabase :
        IDisposable
    {
        Func<Task> delete;

        public SqlDatabase(string connectionString, string name, Func<Task> delete)
        {
            this.delete = delete;
            Guard.AgainstNullWhiteSpace(nameof(connectionString), connectionString);
            ConnectionString = connectionString;
            Name = name;
            Connection = new SqlConnection(connectionString);
        }

        public string ConnectionString { get; }
        public string Name { get; }

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

        public Task Delete()
        {
            Dispose();
            return delete();
        }
    }
}
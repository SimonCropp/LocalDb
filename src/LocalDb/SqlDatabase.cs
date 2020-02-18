using System;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace LocalDb
{
    public class SqlDatabase :
        ISqlDatabase
    {
        Func<Task> delete;

        internal SqlDatabase(string connectionString, string name, Func<Task> delete)
        {
            this.delete = delete;
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
            var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();
            return connection;
        }

        public async Task Start()
        {
            await Connection.OpenAsync();
        }

        public void Dispose()
        {
            Connection.Dispose();
        }

#if(NETSTANDARD2_1)
        public ValueTask DisposeAsync()
        {
            return Connection.DisposeAsync();
        }
#endif

        public async Task Delete()
        {
#if(NETSTANDARD2_1)
            await DisposeAsync();
#else
            Dispose();
#endif
            await delete();
        }
    }
}
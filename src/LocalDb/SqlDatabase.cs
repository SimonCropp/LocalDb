using System;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace LocalDb
{
    public class SqlDatabase :
#if(NET5_0)
        IAsyncDisposable,
#endif
        IDisposable
    {
        Func<Task> delete;

        internal SqlDatabase(string connectionString, string name, Func<Task> delete)
        {
            this.delete = delete;
            ConnectionString = connectionString;
            Name = name;
            Connection = new(connectionString);
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
            SqlConnection connection = new(ConnectionString);
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

#if(!NETSTANDARD2_0)
        public ValueTask DisposeAsync()
        {
            return Connection.DisposeAsync();
        }
#endif

        public async Task Delete()
        {
#if(NETSTANDARD2_0)
            Dispose();
#else
            await DisposeAsync();
#endif
            await delete();
        }
    }
}
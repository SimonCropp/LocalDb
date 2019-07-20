using System;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace LocalDb
{
    public class SqlDatabase :
        IDisposable
    {
        Func<Task> delete;
        bool withRollback;

        internal SqlDatabase(string connectionString, string name, Func<Task> delete)
        {
            this.delete = delete;
            ConnectionString = connectionString;
            Name = name;
            Connection = new SqlConnection(connectionString);
        }

        internal SqlDatabase(string connectionString)
        {
            ConnectionString = connectionString;
            Name = "withRollback";
            withRollback = true;
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

        public async Task Start()
        {
            await Connection.OpenAsync();
            if (withRollback)
            {
                Transaction = Connection.BeginTransaction();
            }
        }

        public SqlTransaction Transaction { get; private set; }

        public void Dispose()
        {
            Transaction?.Dispose();
            Connection.Dispose();
        }

        public Task Delete()
        {
            if (withRollback)
            {
                throw new Exception("Delete cannot be used when using with rollback.");
            }

            Dispose();
            return delete();
        }
    }
}
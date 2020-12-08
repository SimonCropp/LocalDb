using System.Threading.Tasks;
using System.Transactions;
using Microsoft.Data.SqlClient;

namespace LocalDb
{
    public class SqlDatabaseWithRollback :
        ISqlDatabase
    {
        internal SqlDatabaseWithRollback(string connectionString)
        {
            ConnectionString = connectionString;
            Name = "withRollback";
            Connection = new(connectionString);
            TransactionOptions transactionOptions = new()
            {
                IsolationLevel = IsolationLevel.Snapshot
            };
            Transaction = new CommittableTransaction(transactionOptions);
        }

        public string ConnectionString { get; }
        public string Name { get; }

        public SqlConnection Connection { get; }

        public static implicit operator SqlConnection(SqlDatabaseWithRollback instance)
        {
            Guard.AgainstNull(nameof(instance), instance);
            return instance.Connection;
        }

        public async Task<SqlConnection> OpenNewConnection()
        {
            SqlConnection connection = new(ConnectionString);
            await connection.OpenAsync();
            Connection.EnlistTransaction(Transaction);
            return connection;
        }

        public async Task Start()
        {
            await Connection.OpenAsync();
            Connection.EnlistTransaction(Transaction);
        }

        public Transaction Transaction { get; }

        public void Dispose()
        {
            Transaction.Rollback();
            Transaction.Dispose();
            Connection.Dispose();
        }

#if(!NETSTANDARD2_0)
        public ValueTask DisposeAsync()
        {
            Transaction.Rollback();
            Transaction.Dispose();
            return Connection.DisposeAsync();
        }
#endif
    }
}
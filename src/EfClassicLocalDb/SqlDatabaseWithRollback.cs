using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Transactions;

namespace EfLocalDb
{
    public class SqlDatabaseWithRollback<TDbContext> :
        ISqlDatabase<TDbContext>
        where TDbContext : DbContext
    {
        Func<DbConnection, TDbContext> constructInstance;
        IEnumerable<object>? data;

        internal SqlDatabaseWithRollback(
            string connectionString,
            Func<DbConnection, TDbContext> constructInstance,
            IEnumerable<object> data)
        {
            Name = "withRollback";
            this.constructInstance = constructInstance;
            this.data = data;
            ConnectionString = connectionString;
            var transactionOptions = new TransactionOptions
            {
                IsolationLevel = IsolationLevel.Snapshot
            };
            Transaction = new CommittableTransaction(transactionOptions);
            Connection = new SqlConnection(ConnectionString);
        }

        public string Name { get; }
        public SqlConnection Connection { get; }
        public string ConnectionString { get; }

        public async Task<SqlConnection> OpenNewConnection()
        {
            var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();
            Connection.EnlistTransaction(Transaction);

            return connection;
        }

        public static implicit operator TDbContext(SqlDatabaseWithRollback<TDbContext> instance)
        {
            Guard.AgainstNull(nameof(instance), instance);
            return instance.Context;
        }

        public static implicit operator SqlConnection(SqlDatabaseWithRollback<TDbContext> instance)
        {
            Guard.AgainstNull(nameof(instance), instance);
            return instance.Connection;
        }

        public async Task Start()
        {
            await Connection.OpenAsync();
            Context = NewDbContext();
            if (data != null)
            {
                await this.AddData(data);
            }
        }

        public Transaction Transaction { get; }

        public TDbContext Context { get; private set; } = null!;

        public TDbContext NewDbContext()
        {
            Transaction.Current = Transaction;
            return constructInstance(Connection);
        }

        public void Dispose()
        {
            Transaction.Rollback();
            Transaction.Dispose();

            Context?.Dispose();
            Connection.Dispose();
        }

    }
}
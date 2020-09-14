using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace EfLocalDb
{
    public class SqlDatabaseWithRollback<TDbContext> :
        ISqlDatabase<TDbContext>
        where TDbContext : DbContext
    {
        ConstructInstance<TDbContext> constructInstance;
        IEnumerable<object>? data;
        Action<SqlServerDbContextOptionsBuilder>? sqlOptionsBuilder;

        internal SqlDatabaseWithRollback(
            string connectionString,
            ConstructInstance<TDbContext> constructInstance,
            IEnumerable<object> data,
            Action<SqlServerDbContextOptionsBuilder>? sqlOptionsBuilder)
        {
            Name = "withRollback";
            this.constructInstance = constructInstance;
            this.data = data;
            this.sqlOptionsBuilder = sqlOptionsBuilder;
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
            var builder = DefaultOptionsBuilder.Build<TDbContext>();
            builder.UseSqlServer(Connection, sqlOptionsBuilder);
            var data = constructInstance(builder);
            data.Database.EnlistTransaction(Transaction);
            return data;
        }

        public void Dispose()
        {
            Transaction.Rollback();
            Transaction.Dispose();

            Context?.Dispose();
            Connection.Dispose();
        }

#if(!NETSTANDARD2_0)
        public async ValueTask DisposeAsync()
        {
            Transaction.Rollback();
            Transaction.Dispose();

            if (Context != null)
            {
                await Context.DisposeAsync();
            }

            await Connection.DisposeAsync();
        }
#endif
    }
}
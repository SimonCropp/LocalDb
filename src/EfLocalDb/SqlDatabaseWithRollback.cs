using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

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
            TransactionOptions transactionOptions = new()
            {
                IsolationLevel = IsolationLevel.Snapshot
            };
            Transaction = new CommittableTransaction(transactionOptions);
            Connection = new(ConnectionString);
        }

        public string Name { get; }
        public SqlConnection Connection { get; }
        public string ConnectionString { get; }

        public async Task<SqlConnection> OpenNewConnection()
        {
            SqlConnection connection = new(ConnectionString);
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
            EntityTypes = Context.Model.GetEntityTypes().ToList();
            if (data != null)
            {
                await this.AddData(data);
            }
        }

        public Transaction Transaction { get; }

        public TDbContext Context { get; private set; } = null!;

        public IReadOnlyList<IEntityType> EntityTypes { get; private set; } = null!;

        public TDbContext NewDbContext()
        {
            var builder = DefaultOptionsBuilder.Build<TDbContext>();
            builder.UseSqlServer(Connection, sqlOptionsBuilder);
            var data = constructInstance(builder);
            data.Database.EnlistTransaction(Transaction);
            return data;
        }

        public async ValueTask DisposeAsync()
        {
            Transaction.Rollback();
            Transaction.Dispose();

            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (Context != null)
            {
                await Context.DisposeAsync();
            }

            await Connection.DisposeAsync();
        }
    }
}
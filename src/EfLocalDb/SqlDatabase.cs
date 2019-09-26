using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace EfLocalDb
{
    public class SqlDatabase<TDbContext> :
        IDisposable
        where TDbContext : DbContext
    {
        Func<DbContextOptionsBuilder<TDbContext>, TDbContext> constructInstance;
        Func<Task> delete = () => Task.CompletedTask;
        IEnumerable<object>? data;
        bool withRollback;

        internal SqlDatabase(
            string connectionString,
            string name,
            Func<DbContextOptionsBuilder<TDbContext>, TDbContext> constructInstance,
            Func<Task> delete,
            IEnumerable<object>? data)
        {
            Name = name;
            this.constructInstance = constructInstance;
            this.delete = delete;
            this.data = data;
            ConnectionString = connectionString;
            Connection = new SqlConnection(connectionString);
        }

        internal SqlDatabase(
            string connectionString,
            Func<DbContextOptionsBuilder<TDbContext>, TDbContext> constructInstance,
            IEnumerable<object> data)
        {
            Name = "withRollback";
            withRollback = true;
            this.constructInstance = constructInstance;
            this.data = data;
            ConnectionString = connectionString;
        }

        public string Name { get; }
        public SqlConnection Connection { get; private set; } = null!;
        public string ConnectionString { get; }

        public async Task<SqlConnection> OpenNewConnection()
        {
            var sqlConnection = new SqlConnection(ConnectionString);
            await sqlConnection.OpenAsync();
            if (withRollback)
            {
                Connection.EnlistTransaction(Transaction);
            }

            return sqlConnection;
        }

        public static implicit operator TDbContext(SqlDatabase<TDbContext> instance)
        {
            Guard.AgainstNull(nameof(instance), instance);
            return instance.Context;
        }

        public static implicit operator SqlConnection(SqlDatabase<TDbContext> instance)
        {
            Guard.AgainstNull(nameof(instance), instance);
            return instance.Connection;
        }

        public async Task Start()
        {
            if (withRollback)
            {
                var transactionOptions = new TransactionOptions
                {
                    IsolationLevel = IsolationLevel.Snapshot
                };
                Transaction = new CommittableTransaction(transactionOptions);
            }
            Connection = new SqlConnection(ConnectionString);
            await Connection.OpenAsync();

            Context = NewDbContext();
            if (data != null)
            {
                await AddData(data);
            }
        }

        public Transaction? Transaction { get; private set; }

        public TDbContext Context { get; private set; } = null!;

        public Task AddData(IEnumerable<object> entities)
        {
            Guard.AgainstNull(nameof(entities), entities);
            Context.AddRange(entities);
            return Context.SaveChangesAsync();
        }

        public Task AddData(params object[] entities)
        {
            return AddData((IEnumerable<object>) entities);
        }

        public async Task AddDataUntracked(IEnumerable<object> entities)
        {
            Guard.AgainstNull(nameof(entities), entities);
            await using var context = NewDbContext();
            context.AddRange(entities);
            await context.SaveChangesAsync();
        }

        public Task AddDataUntracked(params object[] entities)
        {
            return AddDataUntracked((IEnumerable<object>) entities);
        }

        public TDbContext NewDbContext()
        {
            var builder = DefaultOptionsBuilder.Build<TDbContext>();
            builder.UseSqlServer(Connection);
            var dbContext = constructInstance(builder);
            if (withRollback)
            {
                dbContext.Database.EnlistTransaction(Transaction);
            }

            return dbContext;
        }

        public void Dispose()
        {
            if (Transaction != null)
            {
                Transaction.Rollback();
                Transaction.Dispose();
            }
            Context?.Dispose();
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
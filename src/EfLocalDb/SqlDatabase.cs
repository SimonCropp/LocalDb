using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace EfLocalDb
{
    public class SqlDatabase<TDbContext> :
        IDisposable
        where TDbContext : DbContext
    {
        Func<DbContextOptionsBuilder<TDbContext>, TDbContext> constructInstance;
        Func<Task> delete;
        IEnumerable<object> data;
        bool withRollback;

        internal SqlDatabase(
            string connectionString,
            string name,
            Func<DbContextOptionsBuilder<TDbContext>, TDbContext> constructInstance,
            Func<Task> delete,
            IEnumerable<object> data)
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
            Connection = new SqlConnection(connectionString);
        }

        public string Name { get; }
        public SqlConnection Connection { get; }
        public string ConnectionString { get; }

        public async Task<SqlConnection> OpenNewConnection()
        {
            var sqlConnection = new SqlConnection(ConnectionString);
            await sqlConnection.OpenAsync();
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
            await Connection.OpenAsync();
            if (withRollback)
            {
                Transaction = Connection.BeginTransaction(IsolationLevel.Snapshot);
            }

            Context = NewDbContext();
            if (data != null)
            {
                await AddData(data);
            }
        }

        public SqlTransaction Transaction { get; private set; }

        public TDbContext Context { get; private set; }

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
            using (var context = NewDbContext())
            {
                context.AddRange(entities);
                await context.SaveChangesAsync();
            }
        }

        public Task AddDataUntracked(params object[] entities)
        {
            return AddDataUntracked((IEnumerable<object>) entities);
        }

        public TDbContext NewDbContext()
        {
            var builder = DefaultOptionsBuilder.Build<TDbContext>();
            builder.UseSqlServer(Connection);
            var context = constructInstance(builder);
            if (Transaction != null)
            {
                context.Database.UseTransaction(Transaction);
            }

            return context;
        }

        public void Dispose()
        {
            Transaction?.Dispose();
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
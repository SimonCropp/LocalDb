using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;

namespace EfLocalDb
{
    public class SqlDatabase<TDbContext> :
        ISqlDatabase<TDbContext>
        where TDbContext : DbContext
    {
        ConstructInstance<TDbContext> constructInstance;
        Func<Task> delete;
        IEnumerable<object>? data;

        internal SqlDatabase(
            string connectionString,
            string name,
            ConstructInstance<TDbContext> constructInstance,
            Func<Task> delete,
            IEnumerable<object>? data)
        {
            Name = name;
            this.constructInstance = constructInstance;
            this.delete = delete;
            this.data = data;
            ConnectionString = connectionString;
            Connection = new(connectionString);
        }

        public string Name { get; }
        public SqlConnection Connection { get; }
        public string ConnectionString { get; }

        public async Task<SqlConnection> OpenNewConnection()
        {
            SqlConnection connection = new(ConnectionString);
            await connection.OpenAsync();
            return connection;
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

            Context = NewDbContext();
            if (data != null)
            {
                await this.AddData(data);
            }
        }

        public TDbContext Context { get; private set; } = null!;

        /// <summary>
        /// Calls <see cref="DbContext.SaveChanges()"/> on <see cref="Context"/>.
        /// </summary>
        public int SaveChanges()
        {
            return Context.SaveChanges();
        }

        /// <summary>
        /// Calls <see cref="DbContext.SaveChangesAsync(CancellationToken)"/> on <see cref="Context"/>.
        /// </summary>
        public Task<int> SaveChangesAsync(CancellationToken cancellation = default)
        {
            return Context.SaveChangesAsync(cancellation);
        }

        public TDbContext NewDbContext()
        {
            return constructInstance(Connection);
        }

        public void Dispose()
        {
            Context?.Dispose();
            Connection.Dispose();
        }

        public async Task Delete()
        {
            Dispose();
            await delete();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EfLocalDb
{
    public class SqlDatabase<TDbContext> :
        ISqlDatabase<TDbContext>
        where TDbContext : DbContext
    {
        ConstructInstance<TDbContext> constructInstance;
        Func<Task> delete;
        IEnumerable<object>? data;
        Action<SqlServerDbContextOptionsBuilder>? sqlOptionsBuilder;

        internal SqlDatabase(
            string connectionString,
            string name,
            ConstructInstance<TDbContext> constructInstance,
            Func<Task> delete,
            IEnumerable<object>? data,
            Action<SqlServerDbContextOptionsBuilder>? sqlOptionsBuilder)
        {
            Name = name;
            this.constructInstance = constructInstance;
            this.delete = delete;
            this.data = data;
            this.sqlOptionsBuilder = sqlOptionsBuilder;
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
            EntityTypes = Context.Model.GetEntityTypes().ToList();
            if (data != null)
            {
                await this.AddData(data);
            }
        }

        public TDbContext Context { get; private set; } = null!;

        /// <summary>
        /// Calls <see cref="DbContext.SaveChanges()"/> on <see cref="Context"/>.
        /// </summary>
        public int SaveChangesAsync()
        {
            return Context.SaveChanges();
        }

        /// <summary>
        /// Calls <see cref="DbContext.SaveChanges(bool)"/> on <see cref="Context"/>.
        /// </summary>
        public int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            return Context.SaveChanges(acceptAllChangesOnSuccess);
        }

        /// <summary>
        /// Calls <see cref="DbContext.SaveChangesAsync(CancellationToken)"/> on <see cref="Context"/>.
        /// </summary>
        public Task<int> SaveChangesAsync(CancellationToken cancellation = default)
        {
            return Context.SaveChangesAsync(cancellation);
        }

        /// <summary>
        /// Calls <see cref="DbContext.SaveChangesAsync(bool, CancellationToken)"/> on <see cref="Context"/>.
        /// </summary>
        public Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellation = default)
        {
            return Context.SaveChangesAsync(acceptAllChangesOnSuccess, cancellation);
        }

        public TDbContext NewDbContext()
        {
            var builder = DefaultOptionsBuilder.Build<TDbContext>();
            builder.UseSqlServer(Connection, sqlOptionsBuilder);
            return constructInstance(builder);
        }

        internal TDbContext NewConnectionOwnedDbContext()
        {
            var builder = DefaultOptionsBuilder.Build<TDbContext>();
            builder.UseSqlServer(Connection.ConnectionString, sqlOptionsBuilder);
            return constructInstance(builder);
        }

        public IReadOnlyList<IEntityType> EntityTypes { get; private set; } = null!;

        public async ValueTask DisposeAsync()
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (Context != null)
            {
                await Context.DisposeAsync();
            }
            await Connection.DisposeAsync();
        }

        public async Task Delete()
        {
            await DisposeAsync();
            await delete();
        }
    }
}
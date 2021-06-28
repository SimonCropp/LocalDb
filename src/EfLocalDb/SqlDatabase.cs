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
    public partial class SqlDatabase<TDbContext> :
        IAsyncDisposable
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
            NoTrackingContext = NewDbContext(QueryTrackingBehavior.NoTracking);
            EntityTypes = Context.Model.GetEntityTypes().ToList();
            if (data != null)
            {
                await AddData(data);
            }
        }

        public TDbContext Context { get; private set; } = null!;
        public TDbContext NoTrackingContext { get; private set; } = null!;

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

        public TDbContext NewDbContext(QueryTrackingBehavior? tracking = null)
        {
            var builder = DefaultOptionsBuilder.Build<TDbContext>();
            builder.UseSqlServer(Connection, sqlOptionsBuilder);

            if (tracking.HasValue)
            {
                builder.UseQueryTrackingBehavior(tracking.Value);
            }

            return constructInstance(builder);
        }

        internal TDbContext NewConnectionOwnedDbContext(QueryTrackingBehavior? tracking = null)
        {
            var builder = DefaultOptionsBuilder.Build<TDbContext>();
            builder.UseSqlServer(Connection.ConnectionString, sqlOptionsBuilder);
            if (tracking.HasValue)
            {
                builder.UseQueryTrackingBehavior(tracking.Value);
            }

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

            if (NoTrackingContext != null)
            {
                await NoTrackingContext.DisposeAsync();
            }

            await Connection.DisposeAsync();
        }

        public async Task Delete()
        {
            await DisposeAsync();
            await delete();
        }

        /// <summary>
        /// Calls <see cref="DbContext.FindAsync(Type,object[])"/> on all entity types and returns all resulting items.
        /// </summary>
        public async Task<object> Find<T>(params object[] keys)
            where T : class
        {
            var result = await NoTrackingContext.Set<T>().FindAsync(keys);
            if (result != null)
            {
                return result;
            }

            var keyString = string.Join(", ", keys);
            throw new("No record found with keys: " + keyString);
        }

        /// <summary>
        /// Calls <see cref="DbContext.FindAsync(Type,object[])"/> on all entity types and returns true if the item exists.
        /// </summary>
        public async Task<bool> Exists<T>(params object[] keys)
            where T : class
        {
            var result = await Set<T>().FindAsync(keys);
            return result != null;
        }

        /// <summary>
        /// Returns <see cref="DbContext.Set{TEntity}()"/> from <see cref="NoTrackingContext"/>.
        /// </summary>
        public DbSet<T> Set<T>() where T : class
        {
            return NoTrackingContext.Set<T>();
        }

        /// <summary>
        /// Calls <see cref="DbContext.FindAsync(Type,object[])"/> on all entity types and returns all resulting items.
        /// </summary>
        public async Task<object> Find(params object[] keys)
        {
            var results = await FindResults(keys);

            if (results.Count == 1)
            {
                return results[0];
            }

            var keyString = string.Join(", ", keys);

            if (results.Count > 1)
            {
                throw new("More than one record found with keys: " + keyString);
            }

            throw new("No record found with keys: " + keyString);
        }

        /// <summary>
        /// Calls <see cref="DbContext.FindAsync(Type,object[])"/> on all entity types and returns true if the item exists.
        /// </summary>
        public async Task<bool> Exists(params object[] keys)
        {
            var results = await FindResults(keys);

            if (results.Count == 1)
            {
                return true;
            }

            if (results.Count > 1)
            {
                var keyString = string.Join(", ", keys);
                throw new("More than one record found with keys: " + keyString);
            }

            return false;
        }

        async Task<List<object>> FindResults(object[] keys)
        {
            var list = new List<object>();

            var inputKeyTypes = keys.Select(x => x.GetType()).ToList();

            var entitiesToQuery = EntityTypes
                .Where(entity =>
                {
                    var primaryKey = entity.FindPrimaryKey();
                    if (primaryKey == null)
                    {
                        return false;
                    }

                    var entityKeys = primaryKey.Properties.Select(x => x.ClrType);
                    return entityKeys.SequenceEqual(inputKeyTypes);
                });

            foreach (var entity in entitiesToQuery)
            {
                var result = await NoTrackingContext.FindAsync(entity.ClrType, keys);
                if (result != null)
                {
                    list.Add(result);
                }
            }

            return list;
        }
    }
}
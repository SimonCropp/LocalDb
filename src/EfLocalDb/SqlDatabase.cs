using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

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
            Connection = new SqlConnection(connectionString);
        }

        public string Name { get; }
        public SqlConnection Connection { get; }
        public string ConnectionString { get; }

        public async Task<SqlConnection> OpenNewConnection()
        {
            var connection = new SqlConnection(ConnectionString);
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

        public TDbContext NewDbContext()
        {
            var builder = DefaultOptionsBuilder.Build<TDbContext>();
            builder.UseSqlServer(Connection, sqlOptionsBuilder);
            return constructInstance(builder);
        }

        public void Dispose()
        {
            Context?.Dispose();
            Connection.Dispose();
        }

#if(!NETSTANDARD2_0)
        public async ValueTask DisposeAsync()
        {
            if (Context != null)
            {
                await Context.DisposeAsync();
            }
            await Connection.DisposeAsync();
        }
#endif
        public async Task Delete()
        {
#if(NETSTANDARD2_1)
            Dispose();
#else
            await DisposeAsync();
#endif
            await delete();
        }
    }
}
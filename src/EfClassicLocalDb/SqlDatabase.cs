using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace EfLocalDb
{
    public class SqlDatabase<TDbContext> :
        ISqlDatabase<TDbContext>
        where TDbContext : DbContext
    {
        Func<DbConnection, TDbContext> constructInstance;
        Func<Task> delete;
        IEnumerable<object>? data;

        internal SqlDatabase(
            string connectionString,
            string name,
            Func<DbConnection, TDbContext> constructInstance,
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
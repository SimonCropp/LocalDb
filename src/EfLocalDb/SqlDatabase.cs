using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace EfLocalDb
{
    public class SqlDatabase<TDbContext>
        where TDbContext : DbContext
    {
        Func<DbContextOptionsBuilder<TDbContext>, TDbContext> constructInstance;

        public SqlDatabase(string connection, Func<DbContextOptionsBuilder<TDbContext>, TDbContext> constructInstance)
        {
            this.constructInstance = constructInstance;
            Connection = connection;
        }

        public string Connection { get; }

        public async Task AddData(IEnumerable<object> entities)
        {
            Guard.AgainstNull(nameof(entities), entities);
            using (var sqlConnection = new SqlConnection(Connection))
            {
                var openAsync = sqlConnection.OpenAsync();
                var builder = DefaultOptionsBuilder.Build<TDbContext>();
                builder.UseSqlServer(sqlConnection);
                using (var dbContext = constructInstance(builder))
                {
                    dbContext.AddRange(entities);
                    await openAsync;
                    await dbContext.SaveChangesAsync();
                }
            }
        }

        public Task AddData(params object[] entities)
        {
            return AddData((IEnumerable<object>) entities);
        }

        public TDbContext NewDbContext()
        {
            var builder = DefaultOptionsBuilder.Build<TDbContext>();
            builder.UseSqlServer(Connection);
            var newDbContext = constructInstance(builder);
            return newDbContext;
        }

        public async Task<SqlConnection> OpenConnection()
        {
            var connection = new SqlConnection(Connection);
            await connection.OpenAsync();
            return connection;
        }
    }
}
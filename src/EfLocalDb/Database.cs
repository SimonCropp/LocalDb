using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace EFLocalDb
{
    public class Database<TDbContext>
        where TDbContext : DbContext
    {
        Func<DbContextOptionsBuilder<TDbContext>, TDbContext> constructInstance;

        public Database(string connection, Func<DbContextOptionsBuilder<TDbContext>, TDbContext> constructInstance)
        {
            this.constructInstance = constructInstance;
            Connection = connection;
        }

        public string Connection { get; }

        public async Task AddSeed(params object[] entities)
        {
            using (var dbContext = NewDbContext())
            {
                dbContext.AddRange(entities);
                await dbContext.SaveChangesAsync();
            }
        }

        public TDbContext NewDbContext()
        {
            var builder = new DbContextOptionsBuilder<TDbContext>();
            builder.UseSqlServer(Connection);
            return constructInstance(builder);
        }
    }
}
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace EfLocalDb
{
    public static class SqlDatabaseExtensions
    {
        public static Task AddData<TDbContext>(this ISqlDatabase<TDbContext> database, IEnumerable<object> entities)
            where TDbContext : DbContext
        {
            Guard.AgainstNull(nameof(entities), entities);
            database.Context.AddRange(entities);
            return database.Context.SaveChangesAsync();
        }

        public static Task AddData<TDbContext>(this ISqlDatabase<TDbContext> database, params object[] entities)
            where TDbContext : DbContext
        {
            return AddData(database, (IEnumerable<object>) entities);
        }

        public static async Task AddDataUntracked<TDbContext>(this ISqlDatabase<TDbContext> database, IEnumerable<object> entities)
            where TDbContext : DbContext
        {
            Guard.AgainstNull(nameof(entities), entities);
            await using var context = database.NewDbContext();
            context.AddRange(entities);
            await context.SaveChangesAsync();
        }

        public static Task AddDataUntracked<TDbContext>(this ISqlDatabase<TDbContext> database, params object[] entities)
            where TDbContext : DbContext
        {
            return AddDataUntracked(database, (IEnumerable<object>) entities);
        }
    }
}
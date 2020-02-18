using System.Collections.Generic;
using System.Data.Entity;
using System.Threading.Tasks;

namespace EfLocalDb
{
    public static class SqlDatabaseExtensions
    {
        public static Task AddData<TDbContext>(this ISqlDatabase<TDbContext> database, IEnumerable<object> entities)
            where TDbContext : DbContext
        {
            Guard.AgainstNull(nameof(entities), entities);
            foreach (var entity in entities)
            {
                database.Context.Set(entity.GetType()).Add(entity);
            }
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
            using var context = database.NewDbContext();
            foreach (var entity in entities)
            {
                database.Context.Set(entity.GetType()).Add(entity);
            }
            await context.SaveChangesAsync();
        }

        public static Task AddDataUntracked<TDbContext>(this ISqlDatabase<TDbContext> database, params object[] entities)
            where TDbContext : DbContext
        {
            return AddDataUntracked(database, (IEnumerable<object>) entities);
        }
    }
}
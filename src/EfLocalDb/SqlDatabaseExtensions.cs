using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
            return Add(database, entities, database.Context);
        }

        static Task Add<TDbContext>(ISqlDatabase<TDbContext> database, IEnumerable<object> entities, TDbContext context)
            where TDbContext : DbContext
        {
            foreach (var entity in entities)
            {
                if (entity is IEnumerable enumerable)
                {
                    var entityType = entity.GetType();
                    if (database.EntityTypes.Any(x => x.ClrType != entityType))
                    {
                        foreach (var nested in enumerable)
                        {
                            context.Add(nested);
                        }

                        continue;
                    }
                }

                context.Add(entity);
            }
            return context.SaveChangesAsync();
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
            await Add(database, entities, context);
        }

        public static Task AddDataUntracked<TDbContext>(this ISqlDatabase<TDbContext> database, params object[] entities)
            where TDbContext : DbContext
        {
            return AddDataUntracked(database, (IEnumerable<object>) entities);
        }
    }
}
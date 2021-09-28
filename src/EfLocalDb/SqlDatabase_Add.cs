using System.Linq;

namespace EfLocalDb
{
    public partial class SqlDatabase<TDbContext>
    {
        public Task AddData(IEnumerable<object> entities)
        {
            return Add(entities, Context);
        }

        Task Add(IEnumerable<object> entities, TDbContext context)
        {
            foreach (var entity in entities)
            {
                if (entity is IEnumerable enumerable)
                {
                    var entityType = entity.GetType();
                    if (EntityTypes.Any(x => x.ClrType != entityType))
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

        public Task AddData(params object[] entities)
        {
            return AddData((IEnumerable<object>)entities);
        }

        public async Task AddDataUntracked(IEnumerable<object> entities)
        {
            await using var context = NewDbContext();
            await Add(entities, context);
        }

        public Task AddDataUntracked(params object[] entities)
        {
            return AddDataUntracked((IEnumerable<object>)entities);
        }
    }
}
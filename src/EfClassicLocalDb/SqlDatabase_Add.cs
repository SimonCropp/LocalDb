using System.Collections.Generic;
using System.Threading.Tasks;

namespace EfLocalDb
{
    public partial class SqlDatabase<TDbContext>
    {
        public Task AddData(IEnumerable<object> entities)
        {
            Guard.AgainstNull(nameof(entities), entities);
            foreach (var entity in entities)
            {
                Context.Set(entity.GetType()).Add(entity);
            }
            return Context.SaveChangesAsync();
        }

        public Task AddData(params object[] entities)
        {
            return AddData((IEnumerable<object>) entities);
        }

        public async Task AddDataUntracked(IEnumerable<object> entities)
        {
            Guard.AgainstNull(nameof(entities), entities);
            using var context = NewDbContext();
            foreach (var entity in entities)
            {
                Context.Set(entity.GetType()).Add(entity);
            }
            await context.SaveChangesAsync();
        }

        public Task AddDataUntracked(params object[] entities)
        {
            return AddDataUntracked((IEnumerable<object>) entities);
        }
    }
}
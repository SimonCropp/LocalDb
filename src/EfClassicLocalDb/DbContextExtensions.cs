using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Threading.Tasks;

namespace EfLocalDb
{
    public static class DbContextExtensions
    {
        public static async Task CreateOnExistingDb<TDbContext>(this TDbContext context)
            where TDbContext : DbContext
        {
            Guard.AgainstNull(nameof(context), context);
            var script = ((IObjectContextAdapter)context).ObjectContext.CreateDatabaseScript();
            try
            {
                await context.Database.ExecuteSqlCommandAsync(script);
            }
            catch (DbException)
            {
                //swallow for already exists
            }
        }
    }
}
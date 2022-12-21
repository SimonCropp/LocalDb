using System.Data.Entity.Infrastructure;

namespace EfLocalDb;

public static class DbContextExtensions
{
    public static async Task CreateOnExistingDb<TDbContext>(this TDbContext context)
        where TDbContext : DbContext
    {
        var script = ((IObjectContextAdapter) context).ObjectContext.CreateDatabaseScript();
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
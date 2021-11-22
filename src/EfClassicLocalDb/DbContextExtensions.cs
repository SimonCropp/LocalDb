using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using Microsoft.Data.SqlClient;

namespace EfLocalDb;

public static class DbContextExtensions
{
    public static async Task CreateOnExistingDb<TDbContext>(this TDbContext context)
        where TDbContext : DbContext
    {
        var script = ((IObjectContextAdapter)context).ObjectContext.CreateDatabaseScript();
        try
        {
            await context.Database.ExecuteSqlCommandAsync(script);
        }
        catch (SqlException)
        {
            //swallow for already exists
        }
    }
}
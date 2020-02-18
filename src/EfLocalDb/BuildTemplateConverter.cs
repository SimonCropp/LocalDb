using System;
using System.Data.Common;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

static class BuildTemplateConverter
{
    public static Func<DbConnection, DbContextOptionsBuilder<TDbContext>, Task> Convert<TDbContext>(
        Func<DbContextOptionsBuilder<TDbContext>, TDbContext> constructInstance,
        Func<TDbContext, Task>? buildTemplate)
        where TDbContext : DbContext
    {
        return async (connection, builder) =>
        {
            await using var dbContext = constructInstance(builder);
            if (buildTemplate == null)
            {
                await dbContext.Database.EnsureCreatedAsync();
            }
            else
            {
                await buildTemplate(dbContext);
            }
        };
    }
}
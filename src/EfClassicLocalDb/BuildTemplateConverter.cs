using System;
using System.Data.Common;
using System.Data.Entity;
using System.Threading.Tasks;
using EfLocalDb;

static class BuildTemplateConverter
{
    public static Func<DbConnection, Task> Convert<TDbContext>(
        Func<DbConnection, TDbContext> constructInstance,
        Func<TDbContext, Task>? buildTemplate)
        where TDbContext : DbContext
    {
        return async connection =>
        {
            using var context = constructInstance(connection);
            if (buildTemplate == null)
            {
                await context.CreateOnExistingDb();
            }
            else
            {
                await buildTemplate(context);
            }
        };
    }
}
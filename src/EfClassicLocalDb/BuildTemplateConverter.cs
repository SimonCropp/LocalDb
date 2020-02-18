using System;
using System.Data.Common;
using System.Data.Entity;
using System.Threading.Tasks;

static class BuildTemplateConverter
{
    public static Func<DbConnection, Task> Convert<TDbContext>(
        Func<DbConnection, TDbContext> constructInstance,
        Func<TDbContext, Task>? buildTemplate)
        where TDbContext : DbContext
    {
        return async connection =>
        {
            using var dbContext = constructInstance(connection);
            if (buildTemplate == null)
            {
                dbContext.Database.CreateIfNotExists();
            }
            else
            {
                await buildTemplate(dbContext);
            }
        };
    }
}
using System;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
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
            using var context = constructInstance(connection);
            if (buildTemplate == null)
            {
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
            else
            {
                await buildTemplate(context);
            }
        };
    }
}
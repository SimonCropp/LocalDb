using System;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

static class BuildTemplateConverter
{
    public static Func<SqlConnection, DbContextOptionsBuilder<TDbContext>, Task> Convert<TDbContext>(
        Func<DbContextOptionsBuilder<TDbContext>, TDbContext> constructInstance,
        Func<TDbContext, Task> buildTemplate)
        where TDbContext : DbContext
    {
        return async (connection, builder) =>
        {
            using (var dbContext = constructInstance(builder))
            {
                if (buildTemplate == null)
                {
                    await dbContext.Database.EnsureCreatedAsync();
                }
                else
                {
                    await buildTemplate(dbContext);
                }
            }
        };
    }
}
using System.Data.Entity;
using EfLocalDb;

static class BuildTemplateConverter
{
    public static TemplateFromConnection Convert<TDbContext>(
        ConstructInstance<TDbContext> constructInstance,
        TemplateFromContext<TDbContext>? buildTemplate)
        where TDbContext : DbContext
    {
        return async connection =>
        {
            using var context = constructInstance(connection);
            if (buildTemplate is null)
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
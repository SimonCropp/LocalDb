using System.Data.Entity;
using EfLocalDb;

static class BuildTemplateConverter
{
    public static TemplateFromConnection Convert<TDbContext>(
        ConstructInstance<TDbContext> constructInstance,
        TemplateFromContext<TDbContext>? buildTemplate)
        where TDbContext : DbContext
    {
        Guard.AgainstNull(nameof(constructInstance), constructInstance);
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
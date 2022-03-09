using EfLocalDb;
using Microsoft.EntityFrameworkCore;

static class BuildTemplateConverter
{
    public static TemplateFromConnection<TDbContext> Convert<TDbContext>(
        ConstructInstance<TDbContext> constructInstance,
        TemplateFromContext<TDbContext>? buildTemplate)
        where TDbContext : DbContext =>
        async (_, builder) =>
        {
            await using var data = constructInstance(builder);
            if (buildTemplate is null)
            {
                await data.Database.EnsureCreatedAsync();
            }
            else
            {
                await buildTemplate(data);
            }
        };
}
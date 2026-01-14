static class BuildTemplateConverter
{
    public static TemplateFromConnection Convert<TDbContext>(
        ConstructInstance<TDbContext> constructInstance,
        TemplateFromContext<TDbContext>? buildTemplate)
        where TDbContext : DbContext =>
        async (connection, cancel) =>
        {
            using var context = constructInstance(connection);
            if (buildTemplate is null)
            {
                await context.CreateOnExistingDb();
            }
            else
            {
                await buildTemplate(context, cancel);
            }
        };
}
#region EfClassicBuildTemplate

public class BuildTemplate
{
    static SqlInstance<BuildTemplateDbContext> sqlInstance;

    static BuildTemplate() =>
        sqlInstance = new(
            constructInstance: connection => new(connection),
            buildTemplate: async context =>
            {
                await context.CreateOnExistingDb();
                var entity = new TheEntity
                {
                    Property = "prop"
                };
                context.TestEntities.Add(entity);
                await context.SaveChangesAsync();
            });

    [Test]
    public async Task Test()
    {
        using var database = await sqlInstance.Build();

        AreEqual(1, database.Context.TestEntities.Count());
    }
}

#endregion
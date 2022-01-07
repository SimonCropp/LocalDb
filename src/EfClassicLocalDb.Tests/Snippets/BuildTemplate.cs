using EfLocalDb;

#region EfClassicBuildTemplate

public class BuildTemplate
{
    static SqlInstance<BuildTemplateDbContext> sqlInstance;

    static BuildTemplate()
    {
        sqlInstance = new SqlInstance<BuildTemplateDbContext>(
            constructInstance: connection => new BuildTemplateDbContext(connection),
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
    }

    [Fact]
    public async Task Test()
    {
        using var database = await sqlInstance.Build();

        Assert.Single(database.Context.TestEntities);
    }
}

#endregion
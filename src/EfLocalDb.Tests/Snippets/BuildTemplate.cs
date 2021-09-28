using EfLocalDb;
using Xunit;

#region EfBuildTemplate

public class BuildTemplate
{
    static SqlInstance<BuildTemplateDbContext> sqlInstance;

    static BuildTemplate()
    {
        sqlInstance = new(
            constructInstance: builder => new(builder.Options),
            buildTemplate: async context =>
            {
                await context.Database.EnsureCreatedAsync();
                TheEntity entity = new()
                {
                    Property = "prop"
                };
                context.Add(entity);
                await context.SaveChangesAsync();
            });
    }

    [Fact]
    public async Task BuildTemplateTest()
    {
        await using var database = await sqlInstance.Build();

        Assert.Single(database.Context.TestEntities);
    }
}

#endregion
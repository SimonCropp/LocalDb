using System.Threading.Tasks;
using EfLocalDb;
using Xunit;

#region EfClassicBuildTemplate

public class BuildTemplate
{
    static SqlInstance<TheDbContext> sqlInstance;

    static BuildTemplate()
    {
        sqlInstance = new(
            constructInstance: connection => new(connection),
            buildTemplate: async context =>
            {
                await context.CreateOnExistingDb();
                TheEntity entity = new()
                {
                    Property = "prop"
                };
                context.TestEntities.Add(entity);
                await context.SaveChangesAsync();
            });
    }

    [Fact]
    public async Task BuildTemplateTest()
    {
        using var database = await sqlInstance.Build();

        Assert.Single(database.Context.TestEntities);
    }
}

#endregion
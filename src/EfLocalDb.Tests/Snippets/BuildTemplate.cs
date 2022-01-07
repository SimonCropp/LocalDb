﻿using EfLocalDb;

#region EfBuildTemplate

public class BuildTemplate
{
    static SqlInstance<BuildTemplateDbContext> sqlInstance;

    static BuildTemplate()
    {
        sqlInstance = new SqlInstance<BuildTemplateDbContext>(
            constructInstance: builder => new BuildTemplateDbContext(builder.Options),
            buildTemplate: async context =>
            {
                await context.Database.EnsureCreatedAsync();
                var entity = new TheEntity
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
﻿namespace StaticConstructor;
#region EfClassicStaticConstructor

public class Tests
{
    static SqlInstance<TheDbContext> sqlInstance;

    static Tests() =>
        sqlInstance = new(
            connection => new(connection));

    [Fact]
    public async Task Test()
    {
        var entity = new TheEntity
        {
            Property = "prop"
        };
        using var database = await sqlInstance.Build([entity]);
        Assert.Single(database.Context.TestEntities);
    }
}

#endregion
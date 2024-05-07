namespace StaticConstructor;

[Collection("Sequential")]
#region EfStaticConstructor

public class Tests
{
    static SqlInstance<TheDbContext> sqlInstance;

    static Tests() =>
        sqlInstance = new(
            builder => new(builder.Options));

    [Fact]
    public async Task Test()
    {
        var entity = new TheEntity
        {
            Property = "prop"
        };
        await using var database = await sqlInstance.Build([entity]);
        Assert.Single(database.Context.TestEntities);
    }
}

#endregion
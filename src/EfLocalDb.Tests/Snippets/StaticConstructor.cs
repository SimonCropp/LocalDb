namespace StaticConstructor;

#region EfStaticConstructor

[TestFixture]
public class Tests
{
    static SqlInstance<TheDbContext> sqlInstance;

    static Tests() =>
        sqlInstance = new(
            builder => new(builder.Options));

    [Test]
    public async Task Test()
    {
        var entity = new TheEntity
        {
            Property = "prop"
        };
        await using var database = await sqlInstance.Build([entity]);
        AreEqual(1, database.Context.TestEntities.Count());
    }
}

#endregion
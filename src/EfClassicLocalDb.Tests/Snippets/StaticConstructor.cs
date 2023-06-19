#if(!NETCOREAPP3_1)
using EfLocalDb;

namespace StaticConstructor;
#region EfClassicStaticConstructor

public class Tests
{
    static SqlInstance<TheDbContext> sqlInstance;

    static Tests() =>
        sqlInstance = new(
            connection => new(connection));

    public async Task Test()
    {
        var entity = new TheEntity
        {
            Property = "prop"
        };
        var data = new List<object>
        {
            entity
        };
        using var database = await sqlInstance.Build(data);
        Assert.Single(database.Context.TestEntities);
    }
}

#endregion
#endif
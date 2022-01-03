using LocalDb;

namespace StaticConstructor;

#region StaticConstructor

public class Tests
{
    static SqlInstance sqlInstance;

    static Tests()
    {
        sqlInstance = new(
            name: "StaticConstructorInstance",
            buildTemplate: TestDbBuilder.CreateTable);
    }

    [Fact]
    public async Task Test()
    {
        await using var database = await sqlInstance.Build();
        await TestDbBuilder.AddData(database.Connection);
        Assert.Single(await TestDbBuilder.GetData(database.Connection));
    }
}

#endregion
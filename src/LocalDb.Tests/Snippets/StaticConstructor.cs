namespace StaticConstructor;

#region StaticConstructor

public class Tests
{
    static SqlInstance sqlInstance = new(
        name: "StaticConstructorInstance",
        buildTemplate: TestDbBuilder.CreateTable);

    [Fact]
    public async Task Test()
    {
        await using var database = await sqlInstance.Build();
        await TestDbBuilder.AddData(database);
        Assert.Single(await TestDbBuilder.GetData(database));
    }
}

#endregion
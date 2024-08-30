namespace StaticConstructor;

#region StaticConstructor

public class Tests
{
    static SqlInstance sqlInstance = new(
        name: "StaticConstructorInstance",
        buildTemplate: TestDbBuilder.CreateTable);

    [Test]
    public async Task Test()
    {
        await using var database = await sqlInstance.Build();
        await TestDbBuilder.AddData(database);
        var data = await TestDbBuilder.GetData(database);
        AreEqual(1, data.Count);
    }
}

#endregion
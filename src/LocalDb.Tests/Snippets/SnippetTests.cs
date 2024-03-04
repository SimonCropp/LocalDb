using LocalDb;

public class SnippetTests
{
    static SqlInstance sqlInstance = new(
        name: "Snippets",
        buildTemplate: TestDbBuilder.CreateTable);

    #region Test

    [Fact]
    public async Task TheTest()
    {
        #region BuildDatabase

        await using var database = await sqlInstance.Build();

        #region BuildContext

        await TestDbBuilder.AddData(database);
        Assert.Single(await TestDbBuilder.GetData(database));

        #endregion

        #endregion
    }

    #endregion

    [Fact]
    public async Task TheTestWithDbName()
    {
        #region WithDbName

        await using var database = await sqlInstance.Build("TheTestWithDbName");

        #endregion

        await TestDbBuilder.AddData(database);
        Assert.Single(await TestDbBuilder.GetData(database));
    }
}
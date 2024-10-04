[TestFixture]
public class SnippetTests
{
    static SqlInstance sqlInstance = new(
        name: "Snippets",
        buildTemplate: TestDbBuilder.CreateTable);

    #region Test

    [Test]
    public async Task TheTest()
    {
        #region BuildDatabase

        await using var database = await sqlInstance.Build();

        #region BuildContext

        await TestDbBuilder.AddData(database);
        var data = await TestDbBuilder.GetData(database);
        Assert.Equals(1, data.Count);

        #endregion

        #endregion
    }

    #endregion

    [Test]
    public async Task TheTestWithDbName()
    {
        #region WithDbName

        await using var database = await sqlInstance.Build("TheTestWithDbName");

        #endregion

        await TestDbBuilder.AddData(database);
        var data = await TestDbBuilder.GetData(database);
        AreEqual(1, data.Count);
    }
}
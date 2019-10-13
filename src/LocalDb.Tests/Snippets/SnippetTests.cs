using System.Threading.Tasks;
using LocalDb;
using Xunit;

public class SnippetTests
{
    static SqlInstance sqlInstance;

    static SnippetTests()
    {
        sqlInstance = new SqlInstance(
            name: "Snippets",
            buildTemplate: TestDbBuilder.CreateTable);
    }

    #region Test

    public async Task TheTest()
    {
        #region BuildDatabase
        await using var database = await sqlInstance.Build();
        #region BuildContext
        await TestDbBuilder.AddData(database.Connection);
        Assert.Single(await TestDbBuilder.GetData(database.Connection));
        #endregion
        #endregion
    }

    #endregion

    public async Task TheTestWithDbName()
    {
        #region WithDbName
        await using var database = await sqlInstance.Build("TheTestWithDbName");
        #endregion
        await TestDbBuilder.AddData(database.Connection);
        Assert.Single(await TestDbBuilder.GetData(database.Connection));
    }
}
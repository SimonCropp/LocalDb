using System.Threading.Tasks;
using LocalDb;
using Xunit;

public class SnippetTests
{
    static SqlInstance instance;

    static SnippetTests()
    {
        instance = new SqlInstance(
            name: "Snippets",
            buildTemplate: TestDbBuilder.CreateTable);
    }

    #region Test

    public async Task TheTest()
    {
        #region BuildLocalDbInstance
        using (var database = await instance.Build())
        {
            #region BuildContext
            await TestDbBuilder.AddData(database.Connection);
            Assert.Single(await TestDbBuilder.GetData(database.Connection));
            #endregion
        }
        #endregion
    }

    #endregion

    public async Task TheTestWithDbName()
    {
        #region WithDbName
        using (var database = await instance.Build("TheTestWithDbName"))
        {
            await TestDbBuilder.AddData(database.Connection);
            Assert.Single(await TestDbBuilder.GetData(database.Connection));
        }
        #endregion
    }
}
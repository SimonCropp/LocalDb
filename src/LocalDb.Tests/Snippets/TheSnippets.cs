using System.Threading.Tasks;
using LocalDb;
using Xunit;

public class TheSnippets
{
    #region Test

    public async Task TheTest()
    {
        #region BuildLocalDbInstance

        using (var database = await SqlInstanceService.Build())
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

        using (var database = await SqlInstanceService.Build("TheTestWithDbName"))
        {
            await TestDbBuilder.AddData(database.Connection);
            Assert.Single(await TestDbBuilder.GetData(database.Connection));
        }
        #endregion
    }
}
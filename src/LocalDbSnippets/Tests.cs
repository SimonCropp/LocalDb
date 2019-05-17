using System.Threading.Tasks;
using LocalDb;
using Xunit;

public class Tests
{
    #region Test

    [Fact]
    public async Task TheTest()
    {
        #region BuildLocalDbInstance

        var database = await SqlInstanceService.Build();

        #endregion

        #region BuildContext

        using (var connection = await database.OpenConnection())
        {
            #endregion
            await TestDbBuilder.AddData(connection);
        }
        
        using (var connection = await database.OpenConnection())
        {
            Assert.Single(await TestDbBuilder.GetData(connection));
        }
    }

    #endregion

    [Fact]
    public async Task TheTestWithDbName()
    {
        #region WithDbName

        var database = await SqlInstanceService.Build("TheTestWithDbName");

        #endregion
        
        using (var connection = await database.OpenConnection())
        {
            await TestDbBuilder.AddData(connection);
        }
        
        using (var connection = await database.OpenConnection())
        {
            Assert.Single(await TestDbBuilder.GetData(connection));
        }
    }
}
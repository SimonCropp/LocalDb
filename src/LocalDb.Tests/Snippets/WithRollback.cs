using System.Threading.Tasks;
using LocalDb;

class WithRollback
{
    async Task Usage()
    {
        #region WithRollback
        SqlInstance sqlInstance = new(
            name: "theInstanceName",
            buildTemplate: TestDbBuilder.CreateTable
        );

        await using var sqlDatabase = await sqlInstance.BuildWithRollback();
        var connection = sqlDatabase.Connection;
        //Use the connection
        #endregion
    }
}
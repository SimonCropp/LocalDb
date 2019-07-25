using System.Threading.Tasks;
using LocalDb;

class WithRollback
{
    async Task Usage()
    {
        #region WithRollback
        var sqlInstance = new SqlInstance(
            name: "theInstanceName",
            buildTemplate: TestDbBuilder.CreateTable
        );

        using (var sqlDatabase = await sqlInstance.BuildWithRollback())
        {
            var sqlConnection = sqlDatabase.Connection;
            //Use the SqlConnection
        }
        #endregion
    }
}
using System.Threading.Tasks;
using EfLocalDb;

class WithRollback
{
    async Task Usage()
    {
        #region EfWithRollback
        var sqlInstance = new SqlInstance<TheDbContext>(
            constructInstance: builder => new TheDbContext(builder.Options));

        await using var sqlDatabase = await sqlInstance.BuildWithRollback();
        var connection = sqlDatabase.Connection;
        var data = sqlDatabase.Context;
        //Use the Connection or TheDbContext
        #endregion
    }
}
using EfLocalDb;

class EfExplicitName
{
    EfExplicitName()
    {
        #region EfExplicitName
        var sqlInstance = new SqlInstance<TheDbContext>(
            constructInstance: builder => new TheDbContext(builder.Options),
            name: "theInstanceName",
            directory: @"C:\LocalDb\theInstance");
        #endregion
    }
}
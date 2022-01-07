using EfLocalDb;

class EfExplicitName
{
    EfExplicitName()
    {
        #region EfExplicitName
        var sqlInstance = new SqlInstance<TheDbContext>(
            constructInstance: builder => new(builder.Options),
            storage: new Storage("theInstanceName", @"C:\LocalDb\theInstance"));
        #endregion
    }
}
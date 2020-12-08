using EfLocalDb;

class EfExplicitName
{
    EfExplicitName()
    {
        #region EfExplicitName
        SqlInstance<TheDbContext> sqlInstance = new(
            constructInstance: builder => new(builder.Options),
            storage: new("theInstanceName",@"C:\LocalDb\theInstance"));
        #endregion
    }
}
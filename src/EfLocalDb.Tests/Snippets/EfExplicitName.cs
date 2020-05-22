using EfLocalDb;

class EfExplicitName
{
    EfExplicitName()
    {
        #region EfExplicitName
        var sqlInstance = new SqlInstance<TheDbContext>(
            constructInstance: builder => new TheDbContext(builder.Options),
            name: new Name<TheDbContext>("theInstanceName",@"C:\LocalDb\theInstance"));
        #endregion
    }
}

// ReSharper disable UnusedVariable

class EfExplicitName
{
    EfExplicitName()
    {
        #region EfExplicitName

        var sqlInstance = new SqlInstance<TheDbContext>(
            constructInstance: builder => new(builder.Options),
            storage: new("theInstanceName", @"C:\LocalDb\theInstance"));

        #endregion
    }
}
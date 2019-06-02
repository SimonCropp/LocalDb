using EfLocalDb;

class Snippets
{
    Snippets()
    {
        #region EfRegisterExplcit

        SqlInstanceService<TheDbContext>.Register(
            constructInstance: builder => new TheDbContext(builder.Options),
            instanceName: "theInstanceName",
            directory: @"C:\LocalDb\theInstance");

        #endregion
    }
}
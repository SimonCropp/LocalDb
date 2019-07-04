using EfLocalDb;

class EfRegisterExplicit
{
    EfRegisterExplicit()
    {
        #region EfRegisterExplicit

        SqlInstanceService<TheDbContext>.Register(
            constructInstance: builder => new TheDbContext(builder.Options),
            instanceName: "theInstanceName",
            directory: @"C:\LocalDb\theInstance");

        #endregion
    }
}
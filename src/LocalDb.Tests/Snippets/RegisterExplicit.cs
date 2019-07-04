using LocalDb;

class RegisterExplicit
{
    RegisterExplicit()
    {
        #region RegisterExplicit

        SqlInstanceService.Register(
            name: "theInstanceName",
            buildTemplate: TestDbBuilder.CreateTable,
            directory: @"C:\LocalDb\theInstance"
        );

        #endregion
    }
}
using LocalDb;

class Snippets
{
    Snippets()
    {
        #region RegisterExplcit

        SqlInstanceService.Register(
            name: "theInstanceName",
            buildTemplate: TestDbBuilder.CreateTable,
            directory: @"C:\EfLocalDb\theInstance"
        );

        #endregion
    }
}
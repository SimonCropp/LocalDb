using LocalDb;

class ExplicitName
{
    ExplicitName()
    {
        #region ExplicitName
        SqlInstance sqlInstance = new(
            name: "theInstanceName",
            buildTemplate: TestDbBuilder.CreateTable,
            directory: @"C:\LocalDb\theInstance"
        );
        #endregion
    }
}
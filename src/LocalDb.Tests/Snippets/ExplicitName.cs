using LocalDb;

class ExplicitName
{
    ExplicitName()
    {
        #region ExplicitName

        var sqlInstance = new SqlInstance(
            name: "theInstanceName",
            buildTemplate: TestDbBuilder.CreateTable,
            directory: @"C:\LocalDb\theInstance");

        #endregion
    }
}
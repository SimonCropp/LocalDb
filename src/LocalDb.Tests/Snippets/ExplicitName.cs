using LocalDb;
// ReSharper disable UnusedVariable

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
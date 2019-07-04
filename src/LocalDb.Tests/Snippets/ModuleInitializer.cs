/**
#region ModuleInitializer

static class ModuleInitializer
{
    public static void Initialize()
    {
        SqlInstanceService.Register(
            name: "MySqlInstance",
            buildTemplate: TestDbBuilder.CreateTable);
    }
}
#endregion
**/
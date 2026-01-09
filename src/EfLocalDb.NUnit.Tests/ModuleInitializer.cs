using EfLocalDb;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        VerifyDiffPlex.Initialize(OutputType.Compact);
        VerifierSettings.InitializePlugins();
        LocalDbLogging.EnableVerbose();
        LocalDbSettings.ConnectionBuilder((instance, database) => $"Data Source=(LocalDb)\\{instance};Database={database};Pooling=true;Connection Timeout=300");
        LocalDbTestBase<TheDbContext>.Initialize();
    }
}
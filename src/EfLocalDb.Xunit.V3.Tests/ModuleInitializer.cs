public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        VerifyDiffPlex.Initialize(OutputType.Compact);
        VerifierSettings.InitializePlugins();
        LocalDbSettings.ConnectionBuilder(_ => _.ConnectTimeout = 300);
        LocalDbTestBase<TheDbContext>.Initialize();
    }
}

[assembly: NonParallelizable]
[assembly: LevelOfParallelism(1)]

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        VerifyDiffPlex.Initialize(OutputType.Compact);
        VerifierSettings.InitializePlugins();
        LocalDbLogging.EnableVerbose();
        LocalDbTestBase<TheDbContext>.Initialize();
    }
}
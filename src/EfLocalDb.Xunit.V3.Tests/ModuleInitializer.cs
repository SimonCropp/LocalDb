public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        // xunit.v3 uses stdout for JSON protocol communication.
        // Redirect Console.Out to stderr to prevent LocalDb logging from breaking the protocol.
        Console.SetOut(Console.Error);

        VerifyDiffPlex.Initialize(OutputType.Compact);
        VerifierSettings.InitializePlugins();
        LocalDbSettings.ConnectionBuilder(_ => _.ConnectTimeout = 300);
        LocalDbTestBase<TheDbContext>.Initialize();
    }
}

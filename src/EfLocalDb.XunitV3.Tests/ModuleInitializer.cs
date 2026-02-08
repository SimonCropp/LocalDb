public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        VerifyDiffPlex.Initialize(OutputType.Compact);
        VerifierSettings.InitializePlugins();
        LocalDbSettings.ConnectionBuilder(_ => _.ConnectTimeout = 300);

        // xUnit v3 uses stdout for JSON protocol communication.
        // Redirect Console.Out during initialization to prevent
        // LocalDb logging from breaking the protocol.
        var original = Console.Out;
        Console.SetOut(TextWriter.Null);
        try
        {
            LocalDbTestBase<TheDbContext>.Initialize();
        }
        finally
        {
            Console.SetOut(original);
        }
    }
}

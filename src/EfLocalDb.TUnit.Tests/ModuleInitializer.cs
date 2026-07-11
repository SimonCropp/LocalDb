public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        VerifierSettings.InitializePlugins();
        // AiCliDetector.Prefix ("chatbot_") is prepended to the LocalDb instance name when
        // running under an AI CLI (e.g. Claude Code). Scrub it so snapshots that capture the
        // instance name / DataSource are stable regardless of environment. No-op otherwise.
        VerifierSettings.AddScrubber(_ => _.Replace("chatbot_", ""));
        LocalDbLogging.EnableVerbose();
        LocalDbSettings.ConnectionBuilder(_ => _.ConnectTimeout = 300);
        LocalDbTestBase<TheDbContext>.Initialize();
    }
}

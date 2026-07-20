public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        VerifierSettings.InitializePlugins();
        LocalDbLogging.EnableVerbose();
        // do not let the test suite sweep the real instance root as a side effect: it would
        // delete instances belonging to other projects on the machine. The sweep is tested
        // directly instead. Must be set before DirectoryFinder initializes
        LocalDbSettings.InstanceCleanupThreshold = TimeSpan.Zero;
        LocalDbSettings.ConnectionBuilder(_ => _.ConnectTimeout = 300);
        VerifierSettings.ScrubLinesContaining("filename = '");
        VerifierSettings.IgnoreMember<LocalDbInstanceInfo>(_ => _.OwnerSID);
        VerifierSettings.IgnoreMember<LocalDbInstanceInfo>(_ => _.Connection);
        VerifierSettings.IgnoreMember<LocalDbInstanceInfo>(_ => _.LastStartUtc);
        VerifierSettings.IgnoreMember<LocalDbInstanceInfo>(_ => _.Build);
        VerifierSettings.IgnoreMember<LocalDbInstanceInfo>(_ => _.Major);
        VerifierSettings.IgnoreMember<LocalDbInstanceInfo>(_ => _.Minor);
        VerifierSettings.IgnoreMember<LocalDbInstanceInfo>(_ => _.Revision);
    }
}

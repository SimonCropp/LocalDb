using VerifyTests.DiffPlex;

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
        LocalDbSettings.ConnectionBuilder((instance, database) => $"Data Source=(LocalDb)\\{instance};Database={database};Pooling=true;Connection Timeout=300");
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
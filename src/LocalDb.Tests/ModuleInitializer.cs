#if EF
using EfLocalDb;

#else
using LocalDb;
#endif

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        VerifierSettings.InitializePlugins();
        XunitContext.Init();
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
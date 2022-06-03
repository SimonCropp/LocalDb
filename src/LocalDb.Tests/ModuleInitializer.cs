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
        XunitContext.Init();
        LocalDbLogging.EnableVerbose();
        LocalDbSettings.ConnectionBuilder((instance, database) => $"Data Source=(LocalDb)\\{instance};Database={database};Pooling=true;Connection Timeout=300");
        VerifierSettings.ScrubLinesContaining("filename = '");
        VerifierSettings.IgnoreMember<LocalDbInstanceInfo>(x => x.OwnerSID);
        VerifierSettings.IgnoreMember<LocalDbInstanceInfo>(x => x.Connection);
        VerifierSettings.IgnoreMember<LocalDbInstanceInfo>(x => x.LastStartUtc);
        VerifierSettings.IgnoreMember<LocalDbInstanceInfo>(x => x.Build);
        VerifierSettings.IgnoreMember<LocalDbInstanceInfo>(x => x.Major);
        VerifierSettings.IgnoreMember<LocalDbInstanceInfo>(x => x.Minor);
        VerifierSettings.IgnoreMember<LocalDbInstanceInfo>(x => x.Revision);
    }
}
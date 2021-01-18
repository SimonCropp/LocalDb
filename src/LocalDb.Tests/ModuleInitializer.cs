using System.Runtime.CompilerServices;
using VerifyTests;
using Xunit;
#if EF
using EfLocalDb;
#else
using LocalDb;
#endif

[assembly: CollectionBehavior(CollectionBehavior.CollectionPerClass, DisableTestParallelization = true)]

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        XunitContext.Init();
        LocalDbLogging.EnableVerbose();
        LocalDbSettings.ConnectionBuilder((instance, database) => $"Data Source=(LocalDb)\\{instance};Database={database};Pooling=true;Connection Timeout=300");
        VerifierSettings.ScrubLinesContaining("filename = '");
        VerifierSettings.ModifySerialization(settings =>
        {
            settings.IgnoreMember<LocalDbInstanceInfo>(x => x.OwnerSID);
            settings.IgnoreMember<LocalDbInstanceInfo>(x => x.Connection);
            settings.IgnoreMember<LocalDbInstanceInfo>(x => x.LastStartUtc);
            settings.IgnoreMember<LocalDbInstanceInfo>(x => x.Build);
            settings.IgnoreMember<LocalDbInstanceInfo>(x => x.Major);
            settings.IgnoreMember<LocalDbInstanceInfo>(x => x.Minor);
            settings.IgnoreMember<LocalDbInstanceInfo>(x => x.Revision);
        });
    }
}
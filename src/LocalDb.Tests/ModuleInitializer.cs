
using VerifyXunit;
using Xunit;

public static class ModuleInitializer
{
    public static void Initialize()
    {
        XunitContext.Init();
        LocalDbLogging.EnableVerbose();  Global.ModifySerialization(settings =>
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
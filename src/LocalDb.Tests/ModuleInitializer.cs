
using VerifyXunit;
using Xunit;

public static class ModuleInitializer
{
    public static void Initialize()
    {
        XunitContext.Init();
        LocalDbLogging.EnableVerbose();
        Global.IgnoreMember<LocalDbInstanceInfo>(x => x.OwnerSID);
        Global.IgnoreMember<LocalDbInstanceInfo>(x => x.Connection);
        Global.IgnoreMember<LocalDbInstanceInfo>(x => x.LastStartUtc);
        Global.IgnoreMember<LocalDbInstanceInfo>(x => x.Build);
        Global.IgnoreMember<LocalDbInstanceInfo>(x => x.Major);
        Global.IgnoreMember<LocalDbInstanceInfo>(x => x.Minor);
        Global.IgnoreMember<LocalDbInstanceInfo>(x => x.Revision);
    }
}
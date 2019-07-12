#if EF
using EfLocalDb;
#else
using LocalDb;
#endif
using ObjectApproval;

public static class ModuleInitializer
{
    public static void Initialize()
    {
        XunitLogging.Init();
        LocalDbLogging.Enable();
        SerializerBuilder.IgnoreMember<LocalDbInstanceInfo>(x => x.OwnerSID);
        SerializerBuilder.IgnoreMember<LocalDbInstanceInfo>(x => x.Connection);
        SerializerBuilder.IgnoreMember<LocalDbInstanceInfo>(x => x.LastStartUtc);
        SerializerBuilder.IgnoreMember<LocalDbInstanceInfo>(x => x.Build);
        SerializerBuilder.IgnoreMember<LocalDbInstanceInfo>(x => x.Major);
        SerializerBuilder.IgnoreMember<LocalDbInstanceInfo>(x => x.Minor);
        SerializerBuilder.IgnoreMember<LocalDbInstanceInfo>(x => x.Revision);
    }
}
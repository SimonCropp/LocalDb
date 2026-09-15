// Calls Initialize from a module initializer, the setup ReportTrxGuard rejects when the
// executable runs with --report-trx. Launched by ReportTrxGuardTests in EfLocalDb.Tests.
public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize() =>
        LocalDbTestBase<HelperDbContext>.Initialize();
}

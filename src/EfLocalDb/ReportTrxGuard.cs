// Microsoft.Testing.Extensions.TrxReport 2.4.0 made --report-trx start the test executable twice:
// as a test host controller and as the test host. A module initializer runs in both processes,
// so LocalDbTestBase<T>.Initialize called from one builds the same LocalDB template twice, and
// the test host fails deleting template files the controller still has attached.
static class ReportTrxGuard
{
    public const string HelpUrl = "https://github.com/SimonCropp/LocalDb/blob/main/pages/report-trx.md";

    public static void ThrowIfModuleInitializer(string setupApi)
    {
        if (!IsReportTrxEnabled(Environment.GetCommandLineArgs()) ||
            !IsInModuleInitializer(new()))
        {
            return;
        }

        throw new($"LocalDbTestBase<T>.Initialize was called from a module initializer while --report-trx is enabled. With --report-trx, Microsoft.Testing.Platform starts the test executable twice, as a test host controller and as the test host, and a module initializer runs in both processes, so both would build the same LocalDB template. Call Initialize from {setupApi} instead. See {HelpUrl}");
    }

    public static bool IsReportTrxEnabled(IEnumerable<string> arguments) =>
        arguments.Any(_ => string.Equals(_, "--report-trx", StringComparison.OrdinalIgnoreCase));

    public static bool IsInModuleInitializer(StackTrace stackTrace) =>
        stackTrace
            .GetFrames()
            .Any(_ => HasModuleInitializerAttribute(_) || IsModuleTypeInitializer(_));

    // The [ModuleInitializer] method itself.
    public static bool HasModuleInitializerAttribute(StackFrame frame) =>
        frame.GetMethod()?.IsDefined(typeof(ModuleInitializerAttribute), false) == true;

    // The <Module> type initializer that invokes module initializers. It stays on the stack
    // even if a module initializer is inlined into it.
    public static bool IsModuleTypeInitializer(StackFrame frame) =>
        frame.GetMethod() is { Name: ".cctor", DeclaringType: null };
}

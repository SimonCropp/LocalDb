[TestFixture]
public class ReportTrxGuardTests
{
    static StackTrace moduleInitializerStack = null!;

    [ModuleInitializer]
    public static void CaptureModuleInitializerStack() =>
        moduleInitializerStack = new();

    [Test]
    public void ModuleInitializerIsDetected()
    {
        True(ReportTrxGuard.IsInModuleInitializer(moduleInitializerStack));
        var frames = moduleInitializerStack.GetFrames();
        True(frames.Any(ReportTrxGuard.HasModuleInitializerAttribute));
        True(frames.Any(ReportTrxGuard.IsModuleTypeInitializer));
    }

    [Test]
    public void OutsideModuleInitializerIsNotDetected() =>
        False(ReportTrxGuard.IsInModuleInitializer(new()));

    [TestCase("--report-trx", true)]
    [TestCase("--REPORT-TRX", true)]
    [TestCase("--report-trx-filename", false)]
    [TestCase("--results-directory", false)]
    public void ReportTrxIsDetected(string argument, bool expected) =>
        AreEqual(expected, ReportTrxGuard.IsReportTrxEnabled([argument]));

    [Test]
    public async Task InitializeFromModuleInitializerWithReportTrxThrows()
    {
        using var resultsDirectory = new TempDirectory();
        var startInfo = new ProcessStartInfo(HelperExeResolver.Resolve("EfLocalDb.ReportTrxHelper"))
        {
            ArgumentList =
            {
                "--report-trx",
                "--results-directory",
                resultsDirectory
            },
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using var process = Process.Start(startInfo)!;
        var standardOutput = process.StandardOutput.ReadToEndAsync();
        var standardError = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }

        var output = await standardOutput + await standardError;
        That(process.ExitCode, Is.Not.Zero, output);
        That(output, Does.Contain("called from a module initializer while --report-trx is enabled"));
        That(output, Does.Contain(ReportTrxGuard.HelpUrl));
    }
}

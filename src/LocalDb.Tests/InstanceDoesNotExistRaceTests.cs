

// Deterministic reproducer for native error 0x89C50107 / LOCALDB_ERROR_INSTANCE_DOES_NOT_EXIST.
//
// Approach: spawn two child processes that race on the LocalDB user-instance metadata for ~5s.
//   * "killer"  — calls LocalDbApi.StopAndDelete(name) in a tight loop.
//   * "victim"  — opens SqlConnection to (LocalDb)\name in a tight loop.
//
// Whenever the killer wins between the victim's instance-name resolution and connection handshake,
// SqlClient surfaces the native LocalDB error 0x89C50107 wrapped in a SqlException whose chain
// includes a Win32Exception with NativeErrorCode == 0x89C50107. The victim exits 0 the first time
// it observes this and the test asserts that outcome.
//
// This is the same family of race that wedges real-world test runs when two test-host processes
// share a LocalDB user instance — there it manifests because two processes running
// Wrapper.InnerStart with no template on disk both call StopAndDelete + CleanStart concurrently.
// The reproducer here is artificial (the killer never recreates the instance) but surfaces the
// *exact* error code, which the symmetric version in MultiProcessConcurrentStartTests does not
// consistently hit.

[TestFixture]
public class InstanceDoesNotExistRaceTests
{
    [Test]
    public async Task KillerVsVictimSurfacesInstanceDoesNotExist()
    {
        const string name = "InstanceDoesNotExistRace";
        const int durationMs = 5000;
        var helperExe = HelperExeResolver.Resolve();

        // Set up a healthy running instance first.
        LocalDbApi.StopAndDelete(name);
        LocalDbApi.CreateInstance(name);
        LocalDbApi.StartInstance(name);

        var signalFile = Path.Combine(Path.GetTempPath(), $"{name}_{Guid.NewGuid():N}.signal");
        if (File.Exists(signalFile))
        {
            File.Delete(signalFile);
        }

        Process? killer = null;
        Process? victim = null;
        try
        {
            killer = StartHelper(helperExe, "killer", name, signalFile, durationMs);
            victim = StartHelper(helperExe, "victim", name, signalFile, durationMs);

            // Let both children reach the signal-wait point, then release simultaneously.
            await Task.Delay(750);
            await File.WriteAllTextAsync(signalFile, "go");

            var killerStdoutTask = killer.StandardOutput.ReadToEndAsync();
            var killerStderrTask = killer.StandardError.ReadToEndAsync();
            var victimStdoutTask = victim.StandardOutput.ReadToEndAsync();
            var victimStderrTask = victim.StandardError.ReadToEndAsync();

            await Task.WhenAll(killer.WaitForExitAsync(), victim.WaitForExitAsync());

            var killerStdout = await killerStdoutTask;
            var killerStderr = await killerStderrTask;
            var victimStdout = await victimStdoutTask;
            var victimStderr = await victimStderrTask;

            if (victim.ExitCode != 0)
            {
                Fail(
                    $"victim exit code {victim.ExitCode} (expected 0 = race observed).{Environment.NewLine}" +
                    $"victim stdout: {victimStdout.Trim()}{Environment.NewLine}" +
                    $"victim stderr: {victimStderr.Trim()}{Environment.NewLine}" +
                    $"killer stdout: {killerStdout.Trim()}{Environment.NewLine}" +
                    $"killer stderr: {killerStderr.Trim()}");
            }

            TestContext.Out.WriteLine(victimStdout.Trim());
            TestContext.Out.WriteLine(killerStdout.Trim());
        }
        finally
        {
            killer?.Dispose();
            victim?.Dispose();
            if (File.Exists(signalFile))
            {
                File.Delete(signalFile);
            }
            LocalDbApi.StopAndDelete(name);
        }
    }

    static Process StartHelper(string helperExe, string mode, string instanceName, string signalFile, int durationMs)
    {
        var psi = new ProcessStartInfo(helperExe)
        {
            ArgumentList = { mode, instanceName, signalFile, durationMs.ToString() },
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        return Process.Start(psi) ?? throw new InvalidOperationException($"Failed to start {helperExe}");
    }
}

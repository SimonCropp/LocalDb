using System.Diagnostics;

// Reproduces the multi-process race surfaced when two test-host processes share a LocalDB
// user instance: each process independently runs Wrapper.InnerStart, which when the template
// files are missing calls LocalDbApi.StopAndDelete + LocalDbApi.CreateInstance + StartInstance.
// One process's StopAndDelete deletes the instance the other is mid-using, surfacing as
// LOCALDB_ERROR_INSTANCE_DOES_NOT_EXIST (native 0x89C50107) or LOCALDB_ERROR_INSTANCE_BUSY.
//
// The single-process variant (ConcurrentStartTests) catches the SQL-DDL deadlock that occurs
// even within one process. This test catches the additional LocalDB-API state race that only
// surfaces across separate Windows processes. Both races dissolve under the same fix:
// serialize Wrapper.InnerStart per-instance with an in-process lock + a named cross-process
// mutex keyed by instance name.

[TestFixture]
public class MultiProcessConcurrentStartTests
{
    [Test]
    public async Task MultiProcessConcurrentStartShouldNotRace()
    {
        const string name = "MultiProcessConcurrentStart";
        const int processCount = 3;
        var directory = DirectoryFinder.Find(name);
        var helperExe = HelperExeResolver.Resolve();

        // Pre-condition that triggers Wrapper.InnerStart's StopAndDelete + CleanStart branch:
        // instance exists, but template.mdf does not.
        LocalDbApi.StopAndDelete(name);
        DirectoryFinder.Delete(name);
        LocalDbApi.CreateInstance(name);
        LocalDbApi.StartInstance(name);

        var signalFile = Path.Combine(Path.GetTempPath(), $"{name}_{Guid.NewGuid():N}.signal");
        if (File.Exists(signalFile))
        {
            File.Delete(signalFile);
        }

        var processes = new List<Process>();
        var outputs = new List<(int ExitCode, string Stdout, string Stderr)>();

        try
        {
            for (var i = 0; i < processCount; i++)
            {
                var psi = new ProcessStartInfo(helperExe)
                {
                    ArgumentList = { "wrapper-start", name, directory, signalFile },
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                processes.Add(Process.Start(psi)!);
            }

            // Give every child enough time to load its CLR, hit the signal-file wait loop, and be ready.
            await Task.Delay(750);
            await File.WriteAllTextAsync(signalFile, "go");

            foreach (var process in processes)
            {
                var stdoutTask = process.StandardOutput.ReadToEndAsync();
                var stderrTask = process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();
                outputs.Add((process.ExitCode, await stdoutTask, await stderrTask));
            }
        }
        finally
        {
            foreach (var process in processes)
            {
                process.Dispose();
            }
            if (File.Exists(signalFile))
            {
                File.Delete(signalFile);
            }
            LocalDbApi.StopAndDelete(name);
            DirectoryFinder.Delete(name);
        }

        var failures = outputs.Where(o => o.ExitCode != 0).ToList();
        if (failures.Count > 0)
        {
            var summary = string.Join(
                Environment.NewLine,
                failures.Select(f => $"  exit {f.ExitCode}: {f.Stderr.Trim().Replace(Environment.NewLine, " | ")}"));
            Assert.Fail($"{failures.Count}/{processCount} child processes failed:{Environment.NewLine}{summary}");
        }
    }
}

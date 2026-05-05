using System.Diagnostics;

// Demonstrates that the AiCliDetector prefix scheme isolates an AI process from a human
// process at the LocalDB-instance level. The "human" child uses the unprefixed name; the
// "AI" child has the CLAUDECODE env var set and runs against the chatbot_-prefixed name.
// They target different LocalDB user instances, so the multi-process StopAndDelete /
// CreateInstance / StartInstance race that previously produced
// LOCALDB_ERROR_INSTANCE_DOES_NOT_EXIST (native 0x89C50107) and instance-busy errors
// no longer has a shared instance to race on.

[TestFixture]
public class MultiProcessConcurrentStartTests
{
    [Test]
    public async Task MultiProcessConcurrentStartShouldNotRace()
    {
        const string humanName = "MultiProcessConcurrentStart";
        var aiName = "chatbot_" + humanName;
        var helperExe = HelperExeResolver.Resolve();

        // Pre-condition that triggers Wrapper.InnerStart's StopAndDelete + CleanStart branch
        // for each instance: instance exists, but template.mdf does not.
        foreach (var instanceName in new[] { humanName, aiName })
        {
            LocalDbApi.StopAndDelete(instanceName);
            DirectoryFinder.Delete(instanceName);
            LocalDbApi.CreateInstance(instanceName);
            LocalDbApi.StartInstance(instanceName);
        }

        var signalFile = Path.Combine(Path.GetTempPath(), $"{humanName}_{Guid.NewGuid():N}.signal");
        if (File.Exists(signalFile))
        {
            File.Delete(signalFile);
        }

        var processes = new List<Process>();
        var outputs = new List<(int ExitCode, string Stdout, string Stderr)>();

        try
        {
            // "Human" child — no AI env var, unprefixed name.
            var humanPsi = new ProcessStartInfo(helperExe)
            {
                ArgumentList = { "wrapper-start", humanName, DirectoryFinder.Find(humanName), signalFile },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            processes.Add(Process.Start(humanPsi)!);

            // "AI" child — CLAUDECODE env var set, runs against the chatbot_-prefixed instance.
            var aiPsi = new ProcessStartInfo(helperExe)
            {
                ArgumentList = { "wrapper-start", aiName, DirectoryFinder.Find(aiName), signalFile },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            aiPsi.Environment["CLAUDECODE"] = "1";
            processes.Add(Process.Start(aiPsi)!);

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
            foreach (var instanceName in new[] { humanName, aiName })
            {
                LocalDbApi.StopAndDelete(instanceName);
                DirectoryFinder.Delete(instanceName);
            }
        }

        var failures = outputs.Where(o => o.ExitCode != 0).ToList();
        if (failures.Count > 0)
        {
            var summary = string.Join(
                Environment.NewLine,
                failures.Select(f => $"  exit {f.ExitCode}: {f.Stderr.Trim().Replace(Environment.NewLine, " | ")}"));
            Assert.Fail($"{failures.Count}/{processes.Count} child processes failed:{Environment.NewLine}{summary}");
        }
    }
}

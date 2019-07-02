using System;
using System.Collections.Generic;
using System.Diagnostics;

static class SqlLocalDb
{
    public static State Start(string instance)
    {
        var localDbInstanceInfo = new ManagedLocalDbApi().GetInstance(instance);

        if (!localDbInstanceInfo.Exists)
        {
            RunLocalDbCommand($"create \"{instance}\" -s");
            return State.NotExists;
        }

        if (!localDbInstanceInfo.IsRunning)
        {
            RunLocalDbCommand($"create \"{instance}\" -s");
        }

        return State.Running;
    }

    public static IEnumerable<string> Instances()
    {
        return RunLocalDbCommand("i");
    }

    public static void DeleteInstance(string instance)
    {
        RunLocalDbCommand($"stop \"{instance}\"");
        RunLocalDbCommand($"delete \"{instance}\"");
    }

    static List<string> RunLocalDbCommand(string command)
    {
        var startInfo = new ProcessStartInfo("sqllocaldb", command)
        {
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        try
        {
            using (var process = Process.Start(startInfo))
            {
                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    var readToEnd = process.StandardError.ReadToEnd();
                    throw new Exception($"ExitCode: {process.ExitCode}. Output: {readToEnd}");
                }

                string line;
                var list = new List<string>();
                while ((line = process.StandardOutput.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }
                    list.Add(line);
                }

                return list;
            }
        }
        catch (Exception exception)
        {
            throw new Exception(
                innerException: exception,
                message: $@"Failed to {nameof(RunLocalDbCommand)}
{nameof(command)}: sqllocaldb {command}
");
        }
    }
}
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

static class SqlLocalDb
{
    public static void Start(string instance)
    {
        RunLocalDbCommand($"create \"{instance}\" -s");
    }

    public static IEnumerable<string> Instances()
    {
        using (var reader = new StringReader(RunLocalDbCommand($"i")))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                yield return line;
            }
        }
    }

    public static void DeleteInstance(string instance)
    {
        RunLocalDbCommand(instance);
        RunLocalDbCommand(instance);
    }

    static string RunLocalDbCommand(string command)
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
                return process.StandardOutput.ReadToEnd();
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
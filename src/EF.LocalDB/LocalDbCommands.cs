using System.Diagnostics;
using System.IO;

public static class LocalDbCommands
{
    public static void ResetLocalDb(string key, string dataDirectory)
    {
        RunLocalDbCommand($"stop \"{key}\"");
        RunLocalDbCommand($"delete \"{key}\"");
        RunLocalDbCommand($"create \"{key}\"");
        RunLocalDbCommand($"start \"{key}\"");

        foreach (var file in Directory.EnumerateFiles(dataDirectory))
        {
            File.Delete(file);
        }
    }

    static void RunLocalDbCommand(string command)
    {
        using (var start = Process.Start("sqllocaldb", command))
        {
            start.WaitForExit();
        }
    }
}
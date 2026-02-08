static class DirectoryCleaner
{
    public static void CleanRoot(string root)
    {
        if (!Directory.Exists(root))
        {
            return;
        }

        foreach (var instanceDirectory in Directory.EnumerateDirectories(root))
        {
            CleanInstance(instanceDirectory);
        }
    }

    public static void CleanInstance(string directory)
    {
        if (!Directory.Exists(directory))
        {
            return;
        }

        var files = GetDbFiles(directory).ToList();

        if (files.Count == 0)
        {
            var cutoff = DateTime.Now.AddHours(-6);
            if (Directory.GetCreationTime(directory) < cutoff)
            {
                Directory.Delete(directory, false);
            }

            return;
        }

        var newestWriteTime = files.Max(File.GetLastWriteTime);

        if (newestWriteTime >= DateTime.Now.AddHours(-6))
        {
            return;
        }

        StopIfRunning(directory);

        foreach (var file in files)
        {
            File.Delete(file);
        }

        if (Directory.GetFileSystemEntries(directory).Length == 0)
        {
            Directory.Delete(directory, false);
        }
    }

    static void StopIfRunning(string directory)
    {
        var instanceName = Path.GetFileName(directory);
        var info = LocalDbApi.GetInstance(instanceName);
        if (info is { Exists: true, IsRunning: true })
        {
            LocalDbApi.StopInstance(instanceName, ShutdownMode.KillProcess);
        }
    }

    static IEnumerable<string> GetDbFiles(string instanceDirectory)
    {
        foreach (var dbFile in Directory.EnumerateFiles(instanceDirectory, "*.mdf"))
        {
            yield return dbFile;
        }

        foreach (var logFile in Directory.EnumerateFiles(instanceDirectory, "*.ldf"))
        {
            yield return logFile;
        }
    }
}

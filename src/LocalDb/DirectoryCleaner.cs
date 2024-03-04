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

        var cutoff = DateTime.Now.AddHours(-6);
        foreach (var file in GetDbFiles(directory))
        {
            if (File.GetLastWriteTime(file) < cutoff)
            {
                File.Delete(file);
            }
        }

        if (Directory.GetFileSystemEntries(directory).Length == 0 &&
            Directory.GetCreationTime(directory) < cutoff)
        {
            Directory.Delete(directory, false);
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

        foreach (var logFile in Directory.EnumerateFiles(instanceDirectory, "*.xel"))
        {
            yield return logFile;
        }

        foreach (var logFile in Directory.EnumerateFiles(instanceDirectory, "*.log"))
        {
            yield return logFile;
        }

        foreach (var logFile in Directory.EnumerateFiles(instanceDirectory, "*.bin"))
        {
            yield return logFile;
        }
    }
}
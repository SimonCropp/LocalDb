using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
        foreach (var file in GetDbFiles(directory))
        {
            if (File.GetLastWriteTime(file) < DateTime.Now.AddDays(-1))
            {
                File.Delete(file);
            }
        }

        if (!Directory.GetFileSystemEntries(directory).Any() &&
            Directory.GetCreationTime(directory) < DateTime.Now.AddDays(-1))
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
    }
}
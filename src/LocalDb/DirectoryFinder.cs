using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

static class DirectoryFinder
{
    static string dataRoot;

    static DirectoryFinder()
    {
        dataRoot = FindDataRoot();
        var root = dataRoot;
        CleanDirectory(root);
    }

    public static void CleanDirectory(string root)
    {
        foreach (var instanceDirectory in Directory.EnumerateDirectories(root))
        {
            foreach (var file in GetDbFiles(instanceDirectory))
            {
                if (File.GetLastWriteTime(file) < DateTime.Now.AddDays(-1))
                {
                    File.Delete(file);
                }
            }

            if (!Directory.GetFileSystemEntries(instanceDirectory).Any())
            {
                Directory.Delete(instanceDirectory, false);
            }
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

    public static string Find(string instanceName)
    {
        if (instanceName == null)
        {
            return dataRoot;
        }

        return Path.Combine(dataRoot, instanceName);
    }

    public static void Delete(string instanceName)
    {
        var directory = Find(instanceName);
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, true);
        }
    }

    static string FindDataRoot()
    {
        var localDbEnv = Environment.GetEnvironmentVariable("LocalDBData");
        if (localDbEnv != null)
        {
            return localDbEnv;
        }

        return Path.Combine(Path.GetTempPath(), "LocalDb");
    }
}
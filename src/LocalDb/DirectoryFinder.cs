using System;
using System.IO;

static class DirectoryFinder
{
    static string dataRoot;

    static DirectoryFinder()
    {
        dataRoot = FindDataRoot();
        var root = dataRoot;
        DirectoryCleaner.CleanRoot(root);
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
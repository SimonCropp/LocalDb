using System;
using System.IO;

static class DirectoryFinder
{
    public static string Find(string instanceName)
    {
        var dataRoot = FindDataRoot();
        if (instanceName == null)
        {
            return dataRoot;
        }

        return Path.Combine(dataRoot, instanceName);
    }

    public static string Delete(string instanceName)
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

        var tfsAgentDirectory = Environment.GetEnvironmentVariable("AGENT_TEMPDIRECTORY");
        if (tfsAgentDirectory != null)
        {
            return Path.Combine(tfsAgentDirectory, "LocalDb");
        }

        return Path.Combine(Path.GetTempPath(), "LocalDb");
    }
}
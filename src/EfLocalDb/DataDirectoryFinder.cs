using System;
using System.IO;

static class DataDirectoryFinder
{
    public static string Find(string scope)
    {
        var dataRoot = FindDataRoot();
        if (scope == null)
        {
            return dataRoot;
        }

        return Path.Combine(dataRoot, scope);
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
            return Path.Combine(tfsAgentDirectory, "EfLocalDb");
        }

        return Path.Combine(Path.GetTempPath(), "EfLocalDb");
    }
}
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

    static string FindDataRoot()
    {
        var localDbEnv = Environment.GetEnvironmentVariable("LocalDBData");
        if (localDbEnv != null)
        {
            Guard.DirectoryExists("LocalDBData", localDbEnv);
            return localDbEnv;
        }

        var tfsAgentDirectory = Environment.GetEnvironmentVariable("AGENT_TEMPDIRECTORY");
        if (tfsAgentDirectory != null)
        {
            Guard.DirectoryExists("AGENT_TEMPDIRECTORY", tfsAgentDirectory);
            var agentTemp = Path.Combine(tfsAgentDirectory, "EfLocalDb");
            Directory.CreateDirectory(agentTemp);
            return agentTemp;
        }

        var tempRoot = Path.Combine(Path.GetTempPath(), "EfLocalDb");
        Directory.CreateDirectory(tempRoot);
        return tempRoot;
    }
}
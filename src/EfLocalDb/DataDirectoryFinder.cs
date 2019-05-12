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

    private static string FindDataRoot()
    {
        var efLocalDbEnv = Environment.GetEnvironmentVariable("LocalDBData");
        if (efLocalDbEnv != null)
        {
            return efLocalDbEnv;
        }

        var tfsAgentDirectory = Environment.GetEnvironmentVariable("AGENT_TEMPDIRECTORY");
        if (tfsAgentDirectory != null)
        {
            return Path.Combine(tfsAgentDirectory, "EfLocalDb");
        }

        return Path.Combine(Path.GetTempPath(), "EfLocalDb");
    }
}
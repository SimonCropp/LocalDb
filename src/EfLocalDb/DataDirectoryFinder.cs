using System;
using System.IO;

static class DataDirectoryFinder
{
    public static string Find(string key)
    {
        var dataDirectory = Environment.GetEnvironmentVariable("AGENT_TEMPDIRECTORY");
        dataDirectory = dataDirectory ?? Environment.GetEnvironmentVariable("LocalDBData");
        dataDirectory = dataDirectory ?? Path.GetTempPath();
        dataDirectory = Path.Combine(dataDirectory, key);
        return dataDirectory;
    }
}
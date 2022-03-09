static class DirectoryFinder
{
    public static string dataRoot;

    static DirectoryFinder()
    {
        dataRoot = FindDataRoot();
        var root = dataRoot;
        DirectoryCleaner.CleanRoot(root);
    }

    public static string Find(string instanceName) => Path.Combine(dataRoot, instanceName);

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
        if (localDbEnv is not null)
        {
            return localDbEnv;
        }

        return Path.GetFullPath(Path.Combine(Path.GetTempPath(), "LocalDb"));
    }
}
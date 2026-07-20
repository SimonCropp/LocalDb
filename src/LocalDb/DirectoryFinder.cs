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

    /// <summary>
    /// The directory LocalDB uses for the system databases (master, model, msdb, tempdb), error
    /// logs and traces of each instance. The location is owned by LocalDB and cannot be
    /// configured: it is always derived from the local application data folder.
    /// </summary>
    static string instanceRoot = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Microsoft",
        "Microsoft SQL Server Local DB",
        "Instances");

    public static string FindInstance(string instanceName) => Path.Combine(instanceRoot, instanceName);

    /// <summary>
    /// Deletes the LocalDB owned directory for an instance. Deleting the instance leaves behind
    /// the error logs, traces and event files, so they have to be removed explicitly.
    /// Must only be called once the instance itself has been deleted.
    /// </summary>
    public static void DeleteInstance(string instanceName)
    {
        var directory = FindInstance(instanceName);
        if (!Directory.Exists(directory))
        {
            return;
        }

        try
        {
            Directory.Delete(directory, true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // the engine process can still hold files while it is shutting down.
            // not worth failing a test run over
            LocalDbLogging.LogIfVerbose($"Failed to delete instance directory: {directory}. {exception.Message}");
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
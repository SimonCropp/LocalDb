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
            Clean(instanceDirectory, deleteInstance: true);
        }
    }

    /// <summary>
    /// Cleans the directory of an instance that is about to be started. Only stale data files are
    /// removed: the LocalDB instance is left in place so that it can be reused, since recreating
    /// one is significantly slower than restarting it.
    /// </summary>
    public static void CleanInstance(string directory) =>
        Clean(directory, deleteInstance: false);

    /// <summary>
    /// <paramref name="deleteInstance"/> also removes the LocalDB instance and the system
    /// databases, logs and traces LocalDB keeps for it. Only valid for instances that are not
    /// about to be used, since it forces a full recreate.
    /// </summary>
    static void Clean(string directory, bool deleteInstance)
    {
        if (!Directory.Exists(directory))
        {
            return;
        }

        var files = GetDbFiles(directory).ToList();

        if (files.Count == 0)
        {
            var cutoff = DateTime.Now.AddHours(-6);
            if (Directory.GetCreationTime(directory) < cutoff)
            {
                Directory.Delete(directory, false);
                if (deleteInstance)
                {
                    RemoveInstance(Path.GetFileName(directory));
                }
            }

            return;
        }

        var newestWriteTime = files.Max(File.GetLastWriteTime);

        if (newestWriteTime >= DateTime.Now.AddHours(-6))
        {
            return;
        }

        // both paths release the lock the running engine holds on the data files
        if (deleteInstance)
        {
            RemoveInstance(Path.GetFileName(directory));
        }
        else
        {
            StopIfRunning(directory);
        }

        foreach (var file in files)
        {
            File.Delete(file);
        }

        if (Directory.GetFileSystemEntries(directory).Length == 0)
        {
            Directory.Delete(directory, false);
        }
    }

    /// <summary>
    /// Stops and deletes an instance, then removes the directory LocalDB keeps for it. Deleting
    /// the instance only reclaims the system databases: the logs and traces are left behind.
    /// </summary>
    public static void RemoveInstance(string instanceName)
    {
        var info = LocalDbApi.GetInstance(instanceName);
        if (info.Exists)
        {
            if (info.IsRunning)
            {
                LocalDbApi.StopInstance(instanceName, ShutdownMode.KillProcess);
            }

            LocalDbApi.DeleteInstance(instanceName);
        }

        DirectoryFinder.DeleteInstance(instanceName);
    }

    static void StopIfRunning(string directory)
    {
        var instanceName = Path.GetFileName(directory);
        var info = LocalDbApi.GetInstance(instanceName);
        if (info is { Exists: true, IsRunning: true })
        {
            LocalDbApi.StopInstance(instanceName, ShutdownMode.KillProcess);
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

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
    /// Reclaims instances and residue from the directory LocalDB keeps for each instance.
    /// <para>
    /// Covers what <see cref="CleanRoot" /> cannot see: instances whose data directory is gone, so
    /// nothing under the data root points at them, and directories left behind by instances that
    /// were already deleted, since LocalDB does not remove the logs and event files on delete.
    /// </para>
    /// <para>
    /// Only directories untouched for <paramref name="threshold" /> are processed, both to keep the
    /// startup scan cheap and to avoid racing an instance that is being created or is in use.
    /// </para>
    /// </summary>
    public static void CleanInstanceRoot(string instanceRoot, string dataRoot, TimeSpan threshold)
    {
        if (threshold <= TimeSpan.Zero ||
            !Directory.Exists(instanceRoot))
        {
            return;
        }

        var cutoff = DateTime.Now - threshold;

        HashSet<string> registered;
        try
        {
            registered = new(LocalDbApi.GetInstanceNames(), StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception exception)
        {
            LocalDbLogging.LogIfVerbose($"Failed to enumerate instances, skipping instance root cleanup. {exception.Message}");
            return;
        }

        foreach (var directory in Directory.EnumerateDirectories(instanceRoot))
        {
            try
            {
                CleanInstanceDirectory(directory, dataRoot, registered, cutoff);
            }
            catch (Exception exception)
            {
                // cleanup must never break the test run that triggered it
                LocalDbLogging.LogIfVerbose($"Failed to clean instance directory: {directory}. {exception.Message}");
            }
        }
    }

    internal static void CleanInstanceDirectory(string directory, string dataRoot, HashSet<string> registered, DateTime cutoff)
    {
        var name = Path.GetFileName(directory);

        // instances LocalDB creates and manages, never owned by this library
        if (LocalDbCleanup.IsDefaultInstance(name))
        {
            return;
        }

        // recently touched, so either in use or too new to be sure it is abandoned
        if (NewestWrite(directory) >= cutoff)
        {
            return;
        }

        if (!registered.Contains(name))
        {
            // no instance backs this directory: the instance was already deleted and only the
            // logs and event files remain. Safe to remove whatever created it
            Directory.Delete(directory, true);
            return;
        }

        // likely in use by another process
        if (LocalDbApi.GetInstance(name).IsRunning)
        {
            return;
        }

        // still tracked by a data directory, so CleanRoot governs when it is reclaimed
        if (Directory.Exists(Path.Combine(dataRoot, name)))
        {
            return;
        }

        LocalDbLogging.LogIfVerbose($"Removing orphaned instance: {name}");
        RemoveInstance(name);
    }

    static DateTime NewestWrite(string directory)
    {
        var newest = Directory.GetCreationTime(directory);
        foreach (var file in Directory.EnumerateFiles(directory))
        {
            var write = File.GetLastWriteTime(file);
            if (write > newest)
            {
                newest = write;
            }
        }

        return newest;
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

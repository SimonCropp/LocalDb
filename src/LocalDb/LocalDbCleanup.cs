#if EF
namespace EfLocalDb;
#else
namespace LocalDb;
#endif

/// <summary>
/// Immediately removes LocalDB instances left behind by previous runs.
/// <para>
/// The same instances are removed automatically once they have been untouched for
/// <see cref="LocalDbSettings.InstanceCleanupThreshold" />. This is the forced version, for when
/// they should be reclaimed now rather than waiting for that threshold.
/// </para>
/// </summary>
public static class LocalDbCleanup
{
    /// <summary>
    /// Instances that LocalDB creates and manages itself, and that are never owned by this library.
    /// </summary>
    internal static bool IsDefaultInstance(string name) =>
        name == "MSSQLLocalDB" ||
        // automatic instances, eg "v11.0"
        name.Length > 1 && name[0] == 'v' && char.IsDigit(name[1]);

    /// <summary>
    /// Finds instances that have no corresponding data directory. Running instances and the
    /// LocalDB managed default instances are never treated as orphans.
    /// </summary>
    /// <param name="filter">
    /// Applied to each candidate name. Return false to keep an instance. Use this when the machine
    /// has LocalDB instances that were not created by this library.
    /// </param>
    public static IReadOnlyList<string> FindOrphanInstances(Func<string, bool>? filter = null)
    {
        var orphans = new List<string>();
        foreach (var name in LocalDbApi.GetInstanceNames())
        {
            if (IsDefaultInstance(name))
            {
                continue;
            }

            if (Directory.Exists(DirectoryFinder.Find(name)))
            {
                continue;
            }

            // an instance that is currently running is likely in use by another process
            if (LocalDbApi.GetInstance(name).IsRunning)
            {
                continue;
            }

            if (filter != null && !filter(name))
            {
                continue;
            }

            orphans.Add(name);
        }

        return orphans;
    }

    /// <summary>
    /// Deletes the instances returned by <see cref="FindOrphanInstances" /> and the directories
    /// LocalDB keeps for them. Returns the names of the deleted instances.
    /// </summary>
    /// <param name="filter">
    /// Applied to each candidate name. Return false to keep an instance. Use this when the machine
    /// has LocalDB instances that were not created by this library.
    /// </param>
    public static IReadOnlyList<string> DeleteOrphanInstances(Func<string, bool>? filter = null)
    {
        var orphans = FindOrphanInstances(filter);
        foreach (var orphan in orphans)
        {
            LocalDbLogging.LogIfVerbose($"Deleting orphan instance: {orphan}");
            DirectoryCleaner.RemoveInstance(orphan);
        }

        return orphans;
    }
}

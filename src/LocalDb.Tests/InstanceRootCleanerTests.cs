[TestFixture]
public class InstanceRootCleanerTests
{
    static readonly TimeSpan threshold = TimeSpan.FromHours(6);

    static string MakeStaleDir(string root, string name, params string[] files)
    {
        var dir = Path.Combine(root, name);
        Directory.CreateDirectory(dir);
        var old = DateTime.Now.AddDays(-3);
        foreach (var file in files)
        {
            var path = Path.Combine(dir, file);
            File.WriteAllText(path, "x");
            File.SetLastWriteTime(path, old);
        }

        Directory.SetCreationTime(dir, old);
        return dir;
    }

    [Test]
    public void RemovesResidueDirectoryWithNoInstance()
    {
        using var instanceRoot = new TempDirectory();
        using var dataRoot = new TempDirectory();
        // a name that is not a registered instance, holding only leftover diagnostic files
        var dir = MakeStaleDir(instanceRoot, "ResidueNoInstanceTest", "error.log", "log_1.trc");

        DirectoryCleaner.CleanInstanceRoot(instanceRoot, dataRoot, threshold);

        False(Directory.Exists(dir));
    }

    [Test]
    public void LeavesRecentDirectory()
    {
        using var instanceRoot = new TempDirectory();
        using var dataRoot = new TempDirectory();
        var dir = Path.Combine(instanceRoot, "RecentResidueTest");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "error.log"), "x");

        DirectoryCleaner.CleanInstanceRoot(instanceRoot, dataRoot, threshold);

        True(Directory.Exists(dir));
    }

    [Test]
    public void LeavesDefaultInstanceDirectory()
    {
        using var instanceRoot = new TempDirectory();
        using var dataRoot = new TempDirectory();
        var dir = MakeStaleDir(instanceRoot, "MSSQLLocalDB", "error.log");

        DirectoryCleaner.CleanInstanceRoot(instanceRoot, dataRoot, threshold);

        True(Directory.Exists(dir));
    }

    [Test]
    public void LeavesEverythingWhenDisabled()
    {
        using var instanceRoot = new TempDirectory();
        using var dataRoot = new TempDirectory();
        var dir = MakeStaleDir(instanceRoot, "DisabledCleanupTest", "error.log");

        DirectoryCleaner.CleanInstanceRoot(instanceRoot, dataRoot, TimeSpan.Zero);

        True(Directory.Exists(dir));
    }

    // The routing for a real instance is tested per directory rather than through
    // CleanInstanceRoot, so the sweep is never run against the real instance root where it
    // would process every instance on the machine. A cutoff in the future makes every
    // directory count as stale, isolating the branch under test from timing.
    static HashSet<string> Registered() =>
        new(LocalDbApi.GetInstanceNames(), StringComparer.OrdinalIgnoreCase);

    [Test]
    public async Task RemovesStaleOrphanedInstance()
    {
        var name = "OrphanInstanceRootTest";
        LocalDbApi.StopAndDelete(name);

        using var dataRoot = new TempDirectory();
        var directory = Path.Combine(dataRoot, name);
        using var wrapper = new Wrapper(name, directory);
        wrapper.Start(new(2000, 1, 1), TestDbBuilder.CreateTable);
        await wrapper.AwaitStart();

        var instanceDir = DirectoryFinder.FindInstance(name);
        True(LocalDbApi.GetInstance(name).Exists);
        True(Directory.Exists(instanceDir));

        // orphan it: stop and remove the data directory
        LocalDbApi.StopInstance(name, ShutdownMode.KillProcess);
        Directory.Delete(directory, true);

        DirectoryCleaner.CleanInstanceDirectory(instanceDir, dataRoot, Registered(), DateTime.Now.AddMinutes(5));

        False(LocalDbApi.GetInstance(name).Exists);
        False(Directory.Exists(instanceDir));
    }

    [Test]
    public async Task LeavesRunningInstance()
    {
        var name = "RunningInstanceRootTest";
        LocalDbApi.StopAndDelete(name);

        using var dataRoot = new TempDirectory();
        var directory = Path.Combine(dataRoot, name);
        using var wrapper = new Wrapper(name, directory);
        wrapper.Start(new(2000, 1, 1), TestDbBuilder.CreateTable);
        await wrapper.AwaitStart();

        var instanceDir = DirectoryFinder.FindInstance(name);

        // no data directory and treated as stale, but running: must be left alone
        Directory.Delete(directory, true);

        try
        {
            DirectoryCleaner.CleanInstanceDirectory(instanceDir, dataRoot, Registered(), DateTime.Now.AddMinutes(5));

            True(LocalDbApi.GetInstance(name).Exists);
        }
        finally
        {
            DirectoryCleaner.RemoveInstance(name);
        }
    }

    [Test]
    public void LeavesTrackedInstanceWithDataDirectory()
    {
        var name = "TrackedInstanceRootTest";
        LocalDbApi.StopAndDelete(name);
        LocalDbApi.CreateInstance(name);

        using var dataRoot = new TempDirectory();
        // an existing data directory means CleanRoot governs it, so the sweep leaves it
        Directory.CreateDirectory(Path.Combine(dataRoot, name));

        try
        {
            DirectoryCleaner.CleanInstanceDirectory(
                DirectoryFinder.FindInstance(name),
                dataRoot,
                Registered(),
                DateTime.Now.AddMinutes(5));

            True(LocalDbApi.GetInstance(name).Exists);
        }
        finally
        {
            DirectoryCleaner.RemoveInstance(name);
        }
    }
}

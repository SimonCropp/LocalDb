[TestFixture]
public class InstanceRootCleanerTests
{
    static readonly TimeSpan threshold = TimeSpan.FromHours(6);

    static string MakeStaleDir(string root, string name, bool marked, params string[] files)
    {
        var dir = Path.Combine(root, name);
        Directory.CreateDirectory(dir);
        var all = files.ToList();
        if (marked)
        {
            all.Add(".localdbwrapper");
        }

        var old = DateTime.Now.AddDays(-3);
        foreach (var file in all)
        {
            var path = Path.Combine(dir, file);
            File.WriteAllText(path, "x");
            File.SetLastWriteTime(path, old);
        }

        Directory.SetCreationTime(dir, old);
        return dir;
    }

    [Test]
    public void RemovesMarkedResidue()
    {
        using var instanceRoot = new TempDirectory();
        using var dataRoot = new TempDirectory();
        var dir = MakeStaleDir(instanceRoot, "MarkedResidueTest", marked: true, "error.log");

        DirectoryCleaner.CleanInstanceRoot(instanceRoot, dataRoot, threshold, backlogPass: false);

        False(Directory.Exists(dir));
    }

    [Test]
    public void LeavesUnmarkedResidueOutsideBacklogPass()
    {
        using var instanceRoot = new TempDirectory();
        using var dataRoot = new TempDirectory();
        var dir = MakeStaleDir(instanceRoot, "UnmarkedResidueTest", marked: false, "error.log");

        DirectoryCleaner.CleanInstanceRoot(instanceRoot, dataRoot, threshold, backlogPass: false);

        True(Directory.Exists(dir));
    }

    [Test]
    public void RemovesUnmarkedResidueInBacklogPass()
    {
        using var instanceRoot = new TempDirectory();
        using var dataRoot = new TempDirectory();
        var dir = MakeStaleDir(instanceRoot, "BacklogResidueTest", marked: false, "error.log");

        DirectoryCleaner.CleanInstanceRoot(instanceRoot, dataRoot, threshold, backlogPass: true);

        False(Directory.Exists(dir));
    }

    [Test]
    public void LeavesRecentDirectory()
    {
        using var instanceRoot = new TempDirectory();
        using var dataRoot = new TempDirectory();
        var dir = Path.Combine(instanceRoot, "RecentResidueTest");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, ".localdbwrapper"), "");

        DirectoryCleaner.CleanInstanceRoot(instanceRoot, dataRoot, threshold, backlogPass: true);

        True(Directory.Exists(dir));
    }

    [Test]
    public void LeavesDefaultInstanceDirectory()
    {
        using var instanceRoot = new TempDirectory();
        using var dataRoot = new TempDirectory();
        var dir = MakeStaleDir(instanceRoot, "MSSQLLocalDB", marked: true, "error.log");

        DirectoryCleaner.CleanInstanceRoot(instanceRoot, dataRoot, threshold, backlogPass: true);

        True(Directory.Exists(dir));
    }

    [Test]
    public void LeavesEverythingWhenDisabled()
    {
        using var instanceRoot = new TempDirectory();
        using var dataRoot = new TempDirectory();
        var dir = MakeStaleDir(instanceRoot, "DisabledCleanupTest", marked: true, "error.log");

        DirectoryCleaner.CleanInstanceRoot(instanceRoot, dataRoot, TimeSpan.Zero, backlogPass: true);

        True(Directory.Exists(dir));
    }

    // The routing for a real instance is tested per directory rather than through
    // CleanInstanceRoot, so the sweep is never run against the real instance root where it
    // would process every instance on the machine. A cutoff in the future makes every
    // directory count as stale, isolating the branch under test from timing.
    static HashSet<string> Registered() =>
        new(LocalDbApi.GetInstanceNames(), StringComparer.OrdinalIgnoreCase);

    static void Clean(string directory, string dataRoot, bool backlogPass = false) =>
        DirectoryCleaner.CleanInstanceDirectory(
            directory,
            dataRoot,
            Registered(),
            DateTime.Now.AddMinutes(5),
            backlogPass);

    [Test]
    public async Task MarksInstanceItCreates()
    {
        var name = "MarkedOnCreateTest";
        LocalDbApi.StopAndDelete(name);

        using var dataRoot = new TempDirectory();
        using var wrapper = new Wrapper(name, Path.Combine(dataRoot, name));
        wrapper.Start(new(2000, 1, 1), TestDbBuilder.CreateTable);
        await wrapper.AwaitStart();

        try
        {
            True(InstanceMarker.IsMarked(DirectoryFinder.FindInstance(name)));
        }
        finally
        {
            DirectoryCleaner.RemoveInstance(name);
        }
    }

    [Test]
    public async Task RemovesMarkedOrphanedInstance()
    {
        var name = "MarkedOrphanTest";
        LocalDbApi.StopAndDelete(name);

        using var dataRoot = new TempDirectory();
        var directory = Path.Combine(dataRoot, name);
        using var wrapper = new Wrapper(name, directory);
        wrapper.Start(new(2000, 1, 1), TestDbBuilder.CreateTable);
        await wrapper.AwaitStart();

        var instanceDir = DirectoryFinder.FindInstance(name);
        True(LocalDbApi.GetInstance(name).Exists);

        // orphan it: stop and remove the data directory
        LocalDbApi.StopInstance(name, ShutdownMode.KillProcess);
        Directory.Delete(directory, true);

        // marked, so reclaimed without needing the backlog pass
        Clean(instanceDir, dataRoot);

        False(LocalDbApi.GetInstance(name).Exists);
        False(Directory.Exists(instanceDir));
    }

    [Test]
    public async Task LeavesUnmarkedOrphanedInstanceOutsideBacklogPass()
    {
        var name = "UnmarkedOrphanTest";
        LocalDbApi.StopAndDelete(name);

        using var dataRoot = new TempDirectory();
        var directory = Path.Combine(dataRoot, name);
        using var wrapper = new Wrapper(name, directory);
        wrapper.Start(new(2000, 1, 1), TestDbBuilder.CreateTable);
        await wrapper.AwaitStart();

        var instanceDir = DirectoryFinder.FindInstance(name);
        LocalDbApi.StopInstance(name, ShutdownMode.KillProcess);
        Directory.Delete(directory, true);

        // an instance that predates marking
        File.Delete(Path.Combine(instanceDir, ".localdbwrapper"));

        try
        {
            Clean(instanceDir, dataRoot);

            True(LocalDbApi.GetInstance(name).Exists);
        }
        finally
        {
            DirectoryCleaner.RemoveInstance(name);
        }
    }

    [Test]
    public async Task RemovesUnmarkedOrphanWithShrunkModelInBacklogPass()
    {
        var name = "BacklogOrphanTest";
        LocalDbApi.StopAndDelete(name);

        using var dataRoot = new TempDirectory();
        var directory = Path.Combine(dataRoot, name);
        using var wrapper = new Wrapper(name, directory);
        wrapper.Start(new(2000, 1, 1), TestDbBuilder.CreateTable);
        await wrapper.AwaitStart();

        var instanceDir = DirectoryFinder.FindInstance(name);
        LocalDbApi.StopInstance(name, ShutdownMode.KillProcess);
        Directory.Delete(directory, true);
        File.Delete(Path.Combine(instanceDir, ".localdbwrapper"));

        // this library shrinks model when it creates an instance, so the fingerprint holds
        True(InstanceMarker.HasShrunkModel(instanceDir));

        Clean(instanceDir, dataRoot, backlogPass: true);

        False(LocalDbApi.GetInstance(name).Exists);
        False(Directory.Exists(instanceDir));
    }

    [Test]
    public void LeavesUnmarkedOrphanWithoutShrunkModelInBacklogPass()
    {
        var name = "ForeignOrphanTest";
        LocalDbApi.StopAndDelete(name);

        // created through the API alone rather than through a Wrapper, so model is never
        // shrunk: this is what an instance belonging to anything else looks like
        LocalDbApi.CreateInstance(name);
        LocalDbApi.StartInstance(name);
        LocalDbApi.StopInstance(name, ShutdownMode.KillProcess);

        var instanceDir = DirectoryFinder.FindInstance(name);
        File.Delete(Path.Combine(instanceDir, ".localdbwrapper"));
        False(InstanceMarker.HasShrunkModel(instanceDir));

        using var dataRoot = new TempDirectory();
        try
        {
            // orphaned, stale, and the backlog pass is running, but it is not ours
            Clean(instanceDir, dataRoot, backlogPass: true);

            True(LocalDbApi.GetInstance(name).Exists);
            True(Directory.Exists(instanceDir));
        }
        finally
        {
            DirectoryCleaner.RemoveInstance(name);
        }
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

        // no data directory and treated as stale, but running: must be left alone
        Directory.Delete(directory, true);

        try
        {
            Clean(DirectoryFinder.FindInstance(name), dataRoot, backlogPass: true);

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
            Clean(DirectoryFinder.FindInstance(name), dataRoot, backlogPass: true);

            True(LocalDbApi.GetInstance(name).Exists);
        }
        finally
        {
            DirectoryCleaner.RemoveInstance(name);
        }
    }
}

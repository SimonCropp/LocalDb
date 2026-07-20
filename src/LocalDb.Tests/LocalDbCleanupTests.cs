[TestFixture]
public class LocalDbCleanupTests
{
    static void CreateStoppedInstance(string name)
    {
        LocalDbApi.StopAndDelete(name);
        DirectoryFinder.Delete(name);

        LocalDbApi.CreateInstance(name);
        // starting materializes the directory LocalDB keeps for the instance
        LocalDbApi.StartInstance(name);
        LocalDbApi.StopInstance(name, ShutdownMode.KillProcess);
    }

    [Test]
    public void FindsInstanceWithNoDataDirectory()
    {
        var name = "OrphanCleanupTest";
        CreateStoppedInstance(name);

        try
        {
            True(LocalDbCleanup.FindOrphanInstances().Contains(name));
        }
        finally
        {
            DirectoryCleaner.RemoveInstance(name);
        }
    }

    [Test]
    public void SkipsInstanceWithDataDirectory()
    {
        var name = "OrphanCleanupWithDirectoryTest";
        CreateStoppedInstance(name);
        Directory.CreateDirectory(DirectoryFinder.Find(name));

        try
        {
            False(LocalDbCleanup.FindOrphanInstances().Contains(name));
        }
        finally
        {
            DirectoryFinder.Delete(name);
            DirectoryCleaner.RemoveInstance(name);
        }
    }

    [Test]
    public void SkipsDefaultInstances() =>
        False(LocalDbCleanup.FindOrphanInstances().Contains("MSSQLLocalDB"));

    [Test]
    public void DeletesInstanceAndDirectory()
    {
        var name = "OrphanCleanupDeleteTest";
        CreateStoppedInstance(name);

        var instanceDirectory = DirectoryFinder.FindInstance(name);
        True(Directory.Exists(instanceDirectory));

        // the filter keeps the sweep away from instances this test does not own
        var deleted = LocalDbCleanup.DeleteOrphanInstances(_ => _ == name);

        True(deleted.Contains(name));
        False(LocalDbApi.GetInstance(name).Exists);
        False(Directory.Exists(instanceDirectory));
    }

    [Test]
    public void FilterKeepsUnmatchedInstances()
    {
        var name = "OrphanCleanupFilterTest";
        CreateStoppedInstance(name);

        try
        {
            var deleted = LocalDbCleanup.DeleteOrphanInstances(_ => false);

            False(deleted.Any());
            True(LocalDbApi.GetInstance(name).Exists);
        }
        finally
        {
            DirectoryCleaner.RemoveInstance(name);
        }
    }
}

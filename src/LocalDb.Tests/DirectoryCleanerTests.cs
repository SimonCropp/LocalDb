[TestFixture]
public class DirectoryCleanerTests
{
    [Test]
    public void Empty()
    {
        using var tempDir = new TempDirectory();
        DirectoryCleaner.CleanRoot(tempDir);
    }

    [Test]
    public void FileAtRoot()
    {
        using var tempDir = new TempDirectory();
        var fileAtRoot = Path.Combine(tempDir, "file.txt");
        File.WriteAllText(fileAtRoot, "content");
        DirectoryCleaner.CleanRoot(tempDir);
        True(File.Exists(fileAtRoot));
    }

    [Test]
    public void EmptyDirAtRoot()
    {
        using var tempDir = new TempDirectory();
        var dirAtRoot = Path.Combine(tempDir, "Dir");
        Directory.CreateDirectory(dirAtRoot);
        Directory.SetCreationTime(dirAtRoot, DateTime.Now.AddDays(-3));
        DirectoryCleaner.CleanRoot(tempDir);
        False(Directory.Exists(dirAtRoot));
    }

    [Test]
    public void NonEmptyDirAtRoot()
    {
        using var tempDir = new TempDirectory();
        var dirAtRoot = Path.Combine(tempDir, "Dir");
        Directory.CreateDirectory(dirAtRoot);
        var file = Path.Combine(dirAtRoot, "file.txt");
        File.WriteAllText(file, "content");
        DirectoryCleaner.CleanRoot(tempDir);
        True(Directory.Exists(dirAtRoot));
        True(File.Exists(file));

    }

    [Test]
    public void OldDbFiles()
    {
        using var tempDir = new TempDirectory();
        var dirAtRoot = Path.Combine(tempDir, "Dir");
        Directory.CreateDirectory(dirAtRoot);
        var mdfFile = Path.Combine(dirAtRoot, "file.mdf");
        File.WriteAllText(mdfFile, "content");
        File.SetLastWriteTime(mdfFile, DateTime.Now.AddDays(-3));
        var ldfFile = Path.Combine(dirAtRoot, "file.ldf");
        File.WriteAllText(ldfFile, "content");
        File.SetLastWriteTime(ldfFile, DateTime.Now.AddDays(-3));
        Directory.SetCreationTime(dirAtRoot, DateTime.Now.AddDays(-3));
        DirectoryCleaner.CleanRoot(tempDir);
        False(Directory.Exists(dirAtRoot));
        False(File.Exists(ldfFile));
        False(File.Exists(mdfFile));
    }

    [Test]
    public void CurrentDbFiles()
    {
        using var tempDir = new TempDirectory();
        var dirAtRoot = Path.Combine(tempDir, "Dir");
        Directory.CreateDirectory(dirAtRoot);
        var mdfFile = Path.Combine(dirAtRoot, "file.mdf");
        File.WriteAllText(mdfFile, "content");
        var ldfFile = Path.Combine(dirAtRoot, "file.ldf");
        File.WriteAllText(ldfFile, "content");
        DirectoryCleaner.CleanRoot(tempDir);
        True(Directory.Exists(dirAtRoot));
        True(File.Exists(ldfFile));
        True(File.Exists(mdfFile));
    }

    [Test]
    public async Task OldDbFiles_RunningInstance()
    {
        var name = "CleanerRunningInstanceTest";
        LocalDbApi.StopAndDelete(name);
        DirectoryFinder.Delete(name);

        // Create and start a real LocalDB instance so it locks the .mdf/.ldf files
        var directory = DirectoryFinder.Find(name);
        using var wrapper = new Wrapper(name, directory);
        wrapper.Start(new(2000, 1, 1), TestDbBuilder.CreateTable);
        await wrapper.AwaitStart();

        // Verify the instance is running and files exist
        var info = LocalDbApi.GetInstance(name);
        True(info.IsRunning);
        True(File.Exists(wrapper.DataFile));
        True(File.Exists(wrapper.LogFile));

        // Backdate the files so the cleaner considers them stale
        File.SetLastWriteTime(wrapper.DataFile, DateTime.Now.AddDays(-3));
        File.SetLastWriteTime(wrapper.LogFile, DateTime.Now.AddDays(-3));

        // This would throw UnauthorizedAccessException before the fix
        // because the running instance has the files locked
        DirectoryCleaner.CleanInstance(directory);

        // Verify the files were successfully deleted
        False(File.Exists(wrapper.DataFile));
        False(File.Exists(wrapper.LogFile));

        // Verify the instance was stopped
        var infoAfter = LocalDbApi.GetInstance(name);
        False(infoAfter.IsRunning);

        // CleanInstance targets an instance that is about to be started, so the
        // instance itself must survive to avoid a costly recreate
        True(infoAfter.Exists);

        LocalDbApi.StopAndDelete(name);
    }

    [Test]
    public async Task CleanRoot_DeletesStaleInstance()
    {
        var name = "CleanerStaleInstanceTest";
        LocalDbApi.StopAndDelete(name);

        // the instance directory has to live under a root of its own, otherwise the
        // sweep would run over every instance on the machine
        using var tempDir = new TempDirectory();
        var directory = Path.Combine(tempDir, name);
        using var wrapper = new Wrapper(name, directory);
        wrapper.Start(new(2000, 1, 1), TestDbBuilder.CreateTable);
        await wrapper.AwaitStart();

        var instanceDirectory = DirectoryFinder.FindInstance(name);
        True(LocalDbApi.GetInstance(name).Exists);
        True(Directory.Exists(instanceDirectory));

        File.SetLastWriteTime(wrapper.DataFile, DateTime.Now.AddDays(-3));
        File.SetLastWriteTime(wrapper.LogFile, DateTime.Now.AddDays(-3));

        DirectoryCleaner.CleanRoot(tempDir);

        // the sweep targets instances that are not about to be used, so the instance
        // and the directory LocalDB keeps for it are both reclaimed
        False(LocalDbApi.GetInstance(name).Exists);
        False(Directory.Exists(instanceDirectory));
        False(Directory.Exists(directory));
    }

    [Test]
    public void RemoveInstance_DeletesLocalDbDirectory()
    {
        var name = "CleanerRemoveInstanceTest";
        LocalDbApi.CreateInstance(name);
        LocalDbApi.StartInstance(name);

        var instanceDirectory = DirectoryFinder.FindInstance(name);
        True(Directory.Exists(instanceDirectory));

        DirectoryCleaner.RemoveInstance(name);

        False(LocalDbApi.GetInstance(name).Exists);
        // deleting the instance leaves the logs and traces behind, so the
        // directory has to be removed explicitly
        False(Directory.Exists(instanceDirectory));
    }
}

[TestFixture]
public class DirectoryCleanerTests :
    IDisposable
{
    string tempDir;

    [Test]
    public void Empty() => DirectoryCleaner.CleanRoot(tempDir);

    [Test]
    public void FileAtRoot()
    {
        var fileAtRoot = Path.Combine(tempDir, "file.txt");
        File.WriteAllText(fileAtRoot, "content");
        DirectoryCleaner.CleanRoot(tempDir);
        True(File.Exists(fileAtRoot));
    }

    [Test]
    public void EmptyDirAtRoot()
    {
        var dirAtRoot = Path.Combine(tempDir, "Dir");
        Directory.CreateDirectory(dirAtRoot);
        Directory.SetCreationTime(dirAtRoot, DateTime.Now.AddDays(-3));
        DirectoryCleaner.CleanRoot(tempDir);
        False(Directory.Exists(dirAtRoot));
    }

    [Test]
    public void NonEmptyDirAtRoot()
    {
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

    public DirectoryCleanerTests()
    {
        tempDir = Path.Combine(Path.GetTempPath(), "DirectoryCleaner");
        Directory.CreateDirectory(tempDir);
    }

    public void Dispose() => Directory.Delete(tempDir, true);
}
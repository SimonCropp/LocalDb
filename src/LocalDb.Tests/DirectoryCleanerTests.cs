[TestFixture]
public class DirectoryCleanerTests
{
    [Test]
    public void Empty()
    {
        var tempDir = GetTempDir();
        try
        {
            DirectoryCleaner.CleanRoot(tempDir);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    static string GetTempDir()
    {
        var tempDir = Path.GetTempFileName();
        File.Delete(tempDir);
        Directory.CreateDirectory(tempDir);
        return tempDir;
    }

    [Test]
    public void FileAtRoot()
    {
        var tempDir = GetTempDir();
        try
        {
            var fileAtRoot = Path.Combine(tempDir, "file.txt");
            File.WriteAllText(fileAtRoot, "content");
            DirectoryCleaner.CleanRoot(tempDir);
            True(File.Exists(fileAtRoot));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Test]
    public void EmptyDirAtRoot()
    {
        var tempDir = GetTempDir();
        try
        {
            var dirAtRoot = Path.Combine(tempDir, "Dir");
            Directory.CreateDirectory(dirAtRoot);
            Directory.SetCreationTime(dirAtRoot, DateTime.Now.AddDays(-3));
            DirectoryCleaner.CleanRoot(tempDir);
            False(Directory.Exists(dirAtRoot));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Test]
    public void NonEmptyDirAtRoot()
    {
        var tempDir = GetTempDir();
        try
        {
            var dirAtRoot = Path.Combine(tempDir, "Dir");
            Directory.CreateDirectory(dirAtRoot);
            var file = Path.Combine(dirAtRoot, "file.txt");
            File.WriteAllText(file, "content");
            DirectoryCleaner.CleanRoot(tempDir);
            True(Directory.Exists(dirAtRoot));
            True(File.Exists(file));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Test]
    public void OldDbFiles()
    {
        var tempDir = GetTempDir();
        try
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
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Test]
    public void CurrentDbFiles()
    {
        var tempDir = GetTempDir();
        try
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
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}
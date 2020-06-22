using System;
using System.IO;
using Xunit;

public class DirectoryCleanerTests :
    IDisposable
{
    private string tempDir;

    [Fact]
    public void Empty()
    {
        DirectoryCleaner.CleanRoot(tempDir);
    }

    [Fact]
    public void FileAtRoot()
    {
        var fileAtRoot = Path.Combine(tempDir, "file.txt");
        File.WriteAllText(fileAtRoot, "content");
        DirectoryCleaner.CleanRoot(tempDir);
        Assert.True(File.Exists(fileAtRoot));
    }

    [Fact]
    public void EmptyDirAtRoot()
    {
        var dirAtRoot = Path.Combine(tempDir, "Dir");
        Directory.CreateDirectory(dirAtRoot);
        Directory.SetCreationTime(dirAtRoot, DateTime.Now.AddDays(-3));
        DirectoryCleaner.CleanRoot(tempDir);
        Assert.False(Directory.Exists(dirAtRoot));
    }

    [Fact]
    public void NonEmptyDirAtRoot()
    {
        var dirAtRoot = Path.Combine(tempDir, "Dir");
        Directory.CreateDirectory(dirAtRoot);
        var file = Path.Combine(dirAtRoot, "file.txt");
        File.WriteAllText(file, "content");
        DirectoryCleaner.CleanRoot(tempDir);
        Assert.True(Directory.Exists(dirAtRoot));
        Assert.True(File.Exists(file));
    }

    [Fact]
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
        Assert.False(Directory.Exists(dirAtRoot));
        Assert.False(File.Exists(ldfFile));
        Assert.False(File.Exists(mdfFile));
    }

    [Fact]
    public void CurrentDbFiles()
    {
        var dirAtRoot = Path.Combine(tempDir, "Dir");
        Directory.CreateDirectory(dirAtRoot);
        var mdfFile = Path.Combine(dirAtRoot, "file.mdf");
        File.WriteAllText(mdfFile, "content");
        var ldfFile = Path.Combine(dirAtRoot, "file.ldf");
        File.WriteAllText(ldfFile, "content");
        DirectoryCleaner.CleanRoot(tempDir);
        Assert.True(Directory.Exists(dirAtRoot));
        Assert.True(File.Exists(ldfFile));
        Assert.True(File.Exists(mdfFile));
    }

    public DirectoryCleanerTests()
    {
        tempDir = Path.Combine(Path.GetTempPath(), "DirectoryCleaner");
        Directory.CreateDirectory(tempDir);
    }

    public void Dispose()
    {
        Directory.Delete(tempDir, true);
    }
}
using System;
using System.IO;
using System.Threading.Tasks;
using LocalDb;
using VerifyXunit;
using Xunit;
using Xunit.Abstractions;

public class DirectoryFinderTests :
    VerifyBase
{
    private string tempDir;

    [Fact]
    public void Empty()
    {
        DirectoryFinder.CleanDirectory(tempDir);
    }

    [Fact]
    public void FileAtRoot()
    {
        var fileAtRoot = Path.Combine(tempDir, "file.txt");
        File.WriteAllText(fileAtRoot, "content");
        DirectoryFinder.CleanDirectory(tempDir);
        Assert.True(File.Exists(fileAtRoot));
    }

    [Fact]
    public void EmptyDirAtRoot()
    {
        var dirAtRoot = Path.Combine(tempDir, "Dir");
        Directory.CreateDirectory(dirAtRoot);
        DirectoryFinder.CleanDirectory(tempDir);
        Assert.False(Directory.Exists(dirAtRoot));
    }

    [Fact]
    public void NonEmptyDirAtRoot()
    {
        var dirAtRoot = Path.Combine(tempDir, "Dir");
        Directory.CreateDirectory(dirAtRoot);
        var file = Path.Combine(dirAtRoot, "file.txt");
        File.WriteAllText(file, "content");
        DirectoryFinder.CleanDirectory(tempDir);
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
        DirectoryFinder.CleanDirectory(tempDir);
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
        DirectoryFinder.CleanDirectory(tempDir);
        Assert.True(Directory.Exists(dirAtRoot));
        Assert.True(File.Exists(ldfFile));
        Assert.True(File.Exists(mdfFile));
    }

    public DirectoryFinderTests(ITestOutputHelper output) :
        base(output)
    {
        tempDir = Path.Combine(Path.GetTempPath(),"DirectoryFinder");
        Directory.CreateDirectory(tempDir);
    }

    public override void Dispose()
    {
        base.Dispose();
        Directory.Delete(tempDir,true);
    }
}
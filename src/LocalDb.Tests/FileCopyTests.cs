[TestFixture]
public class FileCopyTests
{
    [Test]
    public async Task CopyAsync_CopiesFile()
    {
        using var tempFile = new TempFile();
        var sourceFile = tempFile.Path;
        using var tempDirectory = new TempDirectory();
        var destinationFile = Path.Combine(tempDirectory, "destination.txt");

        await File.WriteAllTextAsync(sourceFile, "test content");

        await File.CopyAsync(sourceFile, destinationFile);

        True(File.Exists(destinationFile));
        AreEqual("test content", await File.ReadAllTextAsync(destinationFile));
    }

    [Test]
    public async Task CopyAsync_CopiesLargeFile()
    {
        using var tempFile = new TempFile();
        var sourceFile = tempFile.Path;
        using var tempDirectory = new TempDirectory();
        var destinationFile = Path.Combine(tempDirectory, "large_destination.bin");

        // Create a 10MB file
        var data = new byte[10 * 1024 * 1024];
        new Random(42).NextBytes(data);
        await File.WriteAllBytesAsync(sourceFile, data);

        await File.CopyAsync(sourceFile, destinationFile);

        True(File.Exists(destinationFile));
        var sourceBytes = await File.ReadAllBytesAsync(sourceFile);
        var destBytes = await File.ReadAllBytesAsync(destinationFile);
        AreEqual(sourceBytes.Length, destBytes.Length);
        IsTrue(sourceBytes.SequenceEqual(destBytes));
    }

    [Test]
    public async Task CopyAsync_PreservesFileContents()
    {
        using var tempFile = new TempFile();
        var sourceFile = tempFile.Path;
        using var tempDirectory = new TempDirectory();
        var destinationFile = Path.Combine(tempDirectory, "destination.bin");

        var testData = "Hello, World! This is a test file with various characters: äöü 日本語 😊"u8.ToArray();
        await File.WriteAllBytesAsync(sourceFile, testData);

        await File.CopyAsync(sourceFile, destinationFile);

        var copiedData = await File.ReadAllBytesAsync(destinationFile);
        IsTrue(testData.SequenceEqual(copiedData));
    }

    [Test]
    public async Task CopyAsync_OverwritesExistingFile()
    {
        using var tempFile1 = new TempFile();
        using var tempFile2 = new TempFile();
        var sourceFile = tempFile1.Path;
        var destinationFile = tempFile2.Path;

        await File.WriteAllTextAsync(sourceFile, "new content");
        await File.WriteAllTextAsync(destinationFile, "old content");

        await File.CopyAsync(sourceFile, destinationFile);

        AreEqual("new content", await File.ReadAllTextAsync(destinationFile));
    }

    [Test]
    public async Task CopyAsync_ParallelCopies()
    {
        using var tempFile1 = new TempFile();
        using var tempFile2 = new TempFile();
        using var tempDirectory = new TempDirectory();

        var sourceFile1 = tempFile1.Path;
        var sourceFile2 = tempFile2.Path;
        var destFile1 = Path.Combine(tempDirectory, "dest1.txt");
        var destFile2 = Path.Combine(tempDirectory, "dest2.txt");

        await File.WriteAllTextAsync(sourceFile1, "content 1");
        await File.WriteAllTextAsync(sourceFile2, "content 2");

        await Task.WhenAll(
            File.CopyAsync(sourceFile1, destFile1),
            File.CopyAsync(sourceFile2, destFile2));

        True(File.Exists(destFile1));
        True(File.Exists(destFile2));
        AreEqual("content 1", await File.ReadAllTextAsync(destFile1));
        AreEqual("content 2", await File.ReadAllTextAsync(destFile2));
    }

    [Test]
    public void CopyAsync_ThrowsWhenSourceDoesNotExist()
    {
        using var tempDirectory = new TempDirectory();
        var sourceFile = Path.Combine(tempDirectory, "nonexistent.txt");
        var destinationFile = Path.Combine(tempDirectory, "destination.txt");

        var exception = ThrowsAsync<IOException>(() =>
            File.CopyAsync(sourceFile, destinationFile));

        IsNotNull(exception);
    }

    [Test]
    public async Task CopyAsync_EmptyFile()
    {
        using var tempFile = new TempFile();
        var sourceFile = tempFile.Path;
        using var tempDirectory = new TempDirectory();
        var destinationFile = Path.Combine(tempDirectory, "empty_copy.txt");

        await File.WriteAllTextAsync(sourceFile, string.Empty);

        await File.CopyAsync(sourceFile, destinationFile);

        True(File.Exists(destinationFile));
        AreEqual(0, new FileInfo(destinationFile).Length);
    }

    [Test]
    public async Task CopyAsync_PreservesFileSize()
    {
        using var tempFile = new TempFile();
        var sourceFile = tempFile.Path;
        using var tempDirectory = new TempDirectory();
        var destinationFile = Path.Combine(tempDirectory, "sized_copy.bin");

        var data = new byte[1234567]; // Arbitrary size
        await File.WriteAllBytesAsync(sourceFile, data);

        await File.CopyAsync(sourceFile, destinationFile);

        AreEqual(new FileInfo(sourceFile).Length, new FileInfo(destinationFile).Length);
    }

    [Test]
    public async Task CopyAsync_MultipleSequentialCopies()
    {
        using var tempFile = new TempFile();
        var sourceFile = tempFile.Path;
        using var tempDirectory = new TempDirectory();

        var dest1 = Path.Combine(tempDirectory, "dest1.txt");
        var dest2 = Path.Combine(tempDirectory, "dest2.txt");
        var dest3 = Path.Combine(tempDirectory, "dest3.txt");

        await File.WriteAllTextAsync(sourceFile, "shared content");

        await File.CopyAsync(sourceFile, dest1);
        await File.CopyAsync(sourceFile, dest2);
        await File.CopyAsync(sourceFile, dest3);

        True(File.Exists(dest1));
        True(File.Exists(dest2));
        True(File.Exists(dest3));
        AreEqual("shared content", await File.ReadAllTextAsync(dest1));
        AreEqual("shared content", await File.ReadAllTextAsync(dest2));
        AreEqual("shared content", await File.ReadAllTextAsync(dest3));
    }
}




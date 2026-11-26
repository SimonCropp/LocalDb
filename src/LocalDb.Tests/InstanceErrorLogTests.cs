using System.Text;

[TestFixture]
public class InstanceErrorLogTests
{
    [Test]
    public void ReadsTail()
    {
        using var directory = new TempDirectory();
        var path = Path.Combine(directory, "error.log");
        // sqlservr writes the log as UTF-16 with a byte order mark
        File.WriteAllLines(path, Enumerable.Range(1, 50).Select(_ => $"line {_}"), Encoding.Unicode);

        var tail = InstanceErrorLog.ReadTailFromFile(path);

        var expected = string.Join("\n", Enumerable.Range(11, InstanceErrorLog.TailLines).Select(_ => $"line {_}"));
        AreEqual($"Last {InstanceErrorLog.TailLines} lines of {path}:\n{expected}", tail);
    }

    [Test]
    public void ReadsShortLog()
    {
        using var directory = new TempDirectory();
        var path = Path.Combine(directory, "error.log");
        File.WriteAllLines(path, ["line 1", "line 2"], Encoding.Unicode);

        var tail = InstanceErrorLog.ReadTailFromFile(path);

        AreEqual($"Last 2 lines of {path}:\nline 1\nline 2", tail);
    }

    [Test]
    public void ReadsLogHeldOpenForWriting()
    {
        using var directory = new TempDirectory();
        var path = Path.Combine(directory, "error.log");
        // as a sqlservr that has not finished shutting down holds it
        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
        using (var writer = new StreamWriter(stream, Encoding.Unicode, 1024, leaveOpen: true))
        {
            writer.WriteLine("line 1");
        }

        var tail = InstanceErrorLog.ReadTailFromFile(path);

        AreEqual($"Last 1 lines of {path}:\nline 1", tail);
    }

    [Test]
    public void MissingLogIsNull()
    {
        using var directory = new TempDirectory();

        IsNull(InstanceErrorLog.ReadTailFromFile(Path.Combine(directory, "error.log")));
    }

    [Test]
    public void EmptyLogIsNull()
    {
        using var directory = new TempDirectory();
        var path = Path.Combine(directory, "error.log");
        File.WriteAllText(path, string.Empty);

        IsNull(InstanceErrorLog.ReadTailFromFile(path));
    }
}

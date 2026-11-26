/// <summary>
/// Reads the error log sqlservr writes into the directory LocalDB keeps for each instance.
/// <para>
/// When sqlservr fails to start, LocalDB only returns LOCALDB_ERROR_SQL_SERVER_STARTUP_FAILED.
/// Why it failed (a locked or corrupt system database, a full disk, a port or memory problem) is
/// only written to this log, so a failure that leaves the log behind on a CI runner is otherwise
/// impossible to diagnose.
/// </para>
/// </summary>
static class InstanceErrorLog
{
    // sqlservr writes the reason it is stopping last, after the startup preamble
    public const int TailLines = 40;

    public static string? ReadTail(string instanceName) =>
        ReadTailFromFile(Path.Combine(DirectoryFinder.FindInstance(instanceName), "error.log"));

    // Best effort: the result only adds detail to an exception that is already being thrown, so
    // it must never replace that exception with one of its own
    public static string? ReadTailFromFile(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return null;
            }

            // a sqlservr that is still shutting down can hold the log open
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            // sqlservr writes the log as UTF-16 with a byte order mark
            using var reader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true);
            var lines = new Queue<string>(TailLines);
            while (reader.ReadLine() is { } line)
            {
                if (lines.Count == TailLines)
                {
                    lines.Dequeue();
                }

                lines.Enqueue(line);
            }

            if (lines.Count == 0)
            {
                return null;
            }

            return $"""
                Last {lines.Count} lines of {path}:
                {string.Join("\n", lines)}
                """;
        }
        catch (Exception exception)
        {
            LocalDbLogging.LogIfVerbose($"Failed to read error log: {path}. {exception.Message}");
            return null;
        }
    }
}

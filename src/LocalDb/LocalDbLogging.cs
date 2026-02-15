#region LocalDbLogging

/// <summary>
///     Controls the logging level.
/// </summary>
public static class LocalDbLogging
{
    /// <summary>
    ///     Enable verbose logging to <see cref="Trace.WriteLine(string)" />
    /// </summary>
    public static void EnableVerbose(bool sqlLogging = false)
    {
        if (WrapperCreated)
        {
            throw new("Must be called prior to `SqlInstance` being created.");
        }

        Enabled = true;
        SqlLoggingEnabled = sqlLogging;
    }

    internal static bool SqlLoggingEnabled;
    internal static bool Enabled;
    internal static bool WrapperCreated;

    internal static void LogIfVerbose(string message)
    {
        if (Enabled)
        {
            Log(message);
        }
    }

    internal static void Log(string message)
    {
        try
        {
            Console.Error.WriteLine($"LocalDb: {message}");
        }
        // dont care if log fails
        catch
        {
        }
    }
}

#endregion
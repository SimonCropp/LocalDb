using System;
using System.Diagnostics;

#region LocalDbLogging
/// <summary>
/// Controls the logging level.
/// </summary>
public static class LocalDbLogging
{
    /// <summary>
    /// Enable verbose logging to <see cref="Trace.WriteLine(string)"/>
    /// </summary>
    public static void EnableVerbose(bool sqlLogging = false)
    {
        if (WrapperCreated)
        {
            throw new Exception("`LocalDbLogging.EnableVerbose()` must be called prior to any `SqlInstance` being created.");
        }
        Enabled = true;
        SqlLoggingEnabled = sqlLogging;
    }

    internal static bool SqlLoggingEnabled;
    internal static bool Enabled;
    internal static bool WrapperCreated;

    internal static void Log(string message)
    {
        if (Enabled)
        {
            Trace.WriteLine(message, "LocalDb");
        }
    }
}
#endregion
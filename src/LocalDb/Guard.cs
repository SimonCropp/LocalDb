static class Guard
{
    internal static bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    static char[] invalidFileNameChars = Path.GetInvalidFileNameChars();

    public static void AgainstInvalidFileName(string value, [CallerArgumentExpression("value")] string? name = null)
    {
        if (value.Any(invalidFileNameChars.Contains))
        {
            throw new ArgumentException($"Invalid file name: {value}", name);
        }
    }

    public static void AgainstBadOS()
    {
        if (!IsWindows)
        {
            throw new("Only windows is supported");
        }
    }

    public static void AgainstDatabaseSize(ushort size, [CallerArgumentExpression("size")] string? name = null)
    {
        if (size < 3)
        {
            throw new ArgumentOutOfRangeException(name, size, "3MB is the min allowed value");
        }
    }

    public static void AgainstZeroShutdownTimeout(ushort timeout, [CallerArgumentExpression("timeout")] string? name = null)
    {
        if (timeout == 0)
        {
            throw new ArgumentOutOfRangeException(name, timeout, "Shutdown timeout must be greater than zero");
        }
    }
}

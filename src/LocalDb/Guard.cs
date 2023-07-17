static class Guard
{
    internal static bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    static char[] invalidFileNameChars = Path.GetInvalidFileNameChars();

    public static void AgainstInvalidFileName(string name, string value)
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

    public static void AgainstDatabaseSize(string name, ushort size)
    {
        if (size < 3)
        {
            throw new ArgumentOutOfRangeException(name, size, "3MB is the min allowed value");
        }
    }

    public static void AgainstNullWhiteSpace(string name, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentNullException(name);
        }
    }

    public static void AgainstWhiteSpace(string name, string? value)
    {
        if (value is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentNullException(name);
        }
    }

    public static void AgainstNegative(string name, int value)
    {
        if (value < 0)
        {
            throw new ArgumentNullException(name);
        }
    }
}
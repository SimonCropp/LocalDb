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

    public static void AgainstNullWhiteSpace(string? value, [CallerArgumentExpression("value")] string? name = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentNullException(name);
        }
    }

    public static void AgainstWhiteSpace(string? value, [CallerArgumentExpression("value")] string? name = null)
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

    public static void AgainstNegative(int value, [CallerArgumentExpression("value")] string? name = null)
    {
        if (value < 0)
        {
            throw new ArgumentNullException(name);
        }
    }
}
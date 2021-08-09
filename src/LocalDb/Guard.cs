using System;
using System.IO;
using System.Linq;

static class Guard
{
    static char[] invalidFileNameChars = Path.GetInvalidFileNameChars();

    public static void AgainstInvalidFileName(string argumentName, string value)
    {
        if (value.Any(x => invalidFileNameChars.Contains(x)))
        {
            throw new ArgumentException($"Invalid file name: {value}", argumentName);
        }
    }

    public static void AgainstDatabaseSize(string argumentName, ushort size)
    {
        if (size < 3)
        {
            throw new ArgumentOutOfRangeException(argumentName, size, "3MB is the min allowed value");
        }
    }

    public static void AgainstNullWhiteSpace(string argumentName, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentNullException(argumentName);
        }
    }

    public static void AgainstWhiteSpace(string argumentName, string? value)
    {
        if (value is null)
        {
            return;
        }
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentNullException(argumentName);
        }
    }

    public static void AgainstNegative(string argumentName, int value)
    {
        if (value < 0)
        {
            throw new ArgumentNullException(argumentName);
        }
    }
}
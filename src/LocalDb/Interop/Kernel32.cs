using System;
using System.Runtime.InteropServices;

static class Kernel32
{
    [Flags]
    public enum LoadLibraryFlags : uint
    {
        DontResolveDllReferences = 0x00000001,
        LoadIgnoreCodeAuthzLevel = 0x00000010,
        LoadLibraryAsDatafile = 0x00000002,
        LoadLibraryAsDatafileExclusive = 0x00000040,
        LoadLibraryAsImageResource = 0x00000020,
        LoadWithAlteredSearchPath = 0x00000008,
        LoadLibrarySearchDllLoadDir = 0x00000100,
        LoadLibrarySearchSystem32 = 0x00000800,
        LoadLibrarySearchDefaultDirs = 0x00001000
    }

    [DllImport("kernel32", SetLastError = true)]
    public static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

    [DllImport("kernel32", SetLastError = true)]
    public static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hReservedNull, LoadLibraryFlags dwFlags);
}
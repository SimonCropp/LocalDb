using System;
using System.IO;
using System.Reflection;

static class AssemblyExtensions
{
    public static string Path(this Assembly assembly)
    {
        return assembly.CodeBase
            .Replace("file:///", "")
            .Replace("file://", "")
            .Replace(@"file:\\\", "")
            .Replace(@"file:\\", "");
    }
    public static DateTime LastModified(this Assembly assembly)
    {
        return File.GetLastWriteTime(assembly.Path());
    }
}
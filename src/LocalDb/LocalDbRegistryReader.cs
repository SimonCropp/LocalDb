﻿using System;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Win32;

static class LocalDbRegistryReader
{
    public static (string path, string version) GetInfo()
    {
        var registryView = GetRegistryView();
        using var rootKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, registryView);
        var versions = rootKey.OpenSubKey(@"SOFTWARE\Microsoft\Microsoft SQL Server Local DB\Installed Versions");
        if (versions == null)
        {
            throw new InvalidOperationException("LocalDb not installed.");
        }

        var latest = versions.GetSubKeyNames()
            .Select(s => new Version(s))
            .OrderBy(s => s)
            .FirstOrDefault();
        if (latest == null)
        {
            throw new InvalidOperationException("LocalDb not installed.");
        }

        using var versionKey = versions.OpenSubKey(latest.ToString());
        if (versionKey == null)
        {
            throw new InvalidOperationException("Could not find LocalDb dll.");
        }

        var version = latest.ToString();
        var path = (string) versionKey.GetValue("InstanceAPIPath");
        return (path, version);
    }

    static RegistryView GetRegistryView()
    {
        var isWow64Process = RuntimeInformation.OSArchitecture == Architecture.X64 &&
                             RuntimeInformation.OSArchitecture == Architecture.X86;
        if (isWow64Process)
        {
            return RegistryView.Registry32;
        }

        return RegistryView.Default;
    }
}
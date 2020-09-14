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
        using var versions = rootKey.OpenSubKey(@"SOFTWARE\Microsoft\Microsoft SQL Server Local DB\Installed Versions");
        if (versions == null)
        {
            throw new InvalidOperationException("LocalDb not installed.");
        }

        var latest = versions.GetSubKeyNames()
            .Select(s => new Version(s))
            .OrderByDescending(s => s)
            .FirstOrDefault();
        if (latest == null)
        {
            throw new InvalidOperationException("LocalDb not installed.");
        }

        using var versionKey = versions.OpenSubKey(latest.ToString());
        if (versionKey == null)
        {
            throw new InvalidOperationException("Could not find LocalDb dll. VersionKey is null");
        }

        var version = latest.ToString();
        var value = versionKey.GetValue("InstanceAPIPath");
        if (value == null)
        {
            throw new InvalidOperationException("Could not find LocalDb dll. No InstanceAPIPath.");
        }
        var path = (string) value;
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
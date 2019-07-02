﻿using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

 public class UnmanagedLocalDbApi
 {
     IntPtr api;

     public UnmanagedLocalDbApi()
     {
         var dllName = GetLocalDbDllName();
         if (dllName == null) throw new InvalidOperationException("Could not find local db dll.");

         api = Kernel32.LoadLibraryEx(dllName, IntPtr.Zero, Kernel32.LoadLibraryFlags.LoadLibrarySearchDefaultDirs);
         if (api == IntPtr.Zero) throw new Win32Exception();

         CreateInstance = GetFunction<LocalDBCreateInstance>();
         DeleteInstance = GetFunction<LocalDBDeleteInstance>();
         FormatMessage = GetFunction<LocalDBFormatMessage>();
         GetInstanceInfo = GetFunction<LocalDBGetInstanceInfo>();
         GetInstances = GetFunction<LocalDBGetInstances>();
         GetVersionInfo = GetFunction<LocalDBGetVersionInfo>();
         GetVersions = GetFunction<LocalDBGetVersions>();
         StartInstance = GetFunction<LocalDBStartInstance>();
         StopInstance = GetFunction<LocalDBStopInstance>();
     }

     public string ApiVersion { get; private set; }
     public const int MaxPath = 260;
     public const int MaxName = 129;
     public const int MaxSid = 187;

     public LocalDBCreateInstance CreateInstance;
     public LocalDBDeleteInstance DeleteInstance;
     public LocalDBFormatMessage FormatMessage;
     public LocalDBGetInstanceInfo GetInstanceInfo;
     public LocalDBGetInstances GetInstances;
     public LocalDBGetVersionInfo GetVersionInfo;
     public LocalDBGetVersions GetVersions;
     public LocalDBStartInstance StartInstance;
     public LocalDBStopInstance StopInstance;

     string GetLocalDbDllName()
     {
         var isWow64Process = RuntimeInformation.OSArchitecture == Architecture.X64 &&
                               RuntimeInformation.OSArchitecture == Architecture.X86;
         var registryView = isWow64Process ? RegistryView.Registry32 : RegistryView.Default;
         using (var rootKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, registryView))
         {
             var versions = rootKey.OpenSubKey(@"SOFTWARE\Microsoft\Microsoft SQL Server Local DB\Installed Versions");
             if (versions == null)
             {
                 throw new InvalidOperationException("LocalDb not installed.");
             }

             var latest = versions.GetSubKeyNames().Select(s => new Version(s)).OrderBy(s => s).FirstOrDefault();
             if (latest == null)
             {
                 throw new InvalidOperationException("LocalDb not installed.");
             }
             using (var versionKey = versions.OpenSubKey(latest.ToString()))
             {
                 ApiVersion = latest.ToString();
                 return (string) versionKey?.GetValue("InstanceAPIPath");
             }
         }
     }

     T GetFunction<T>()
         where T : class
     {
         var name = typeof(T).Name;
         var ptr = Kernel32.GetProcAddress(api, name);
         if (ptr == IntPtr.Zero)
         {
             throw new EntryPointNotFoundException($@"{name}");
         }

         object function = Marshal.GetDelegateForFunctionPointer(ptr, typeof(T));
         return (T) function;
     }

     [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
     public delegate int LocalDBCreateInstance(
         [MarshalAs(UnmanagedType.LPWStr)]
         string wszVersion,
         [MarshalAs(UnmanagedType.LPWStr)]
         string pInstanceName,
         int dwFlags);

     [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
     public delegate int LocalDBDeleteInstance(
         [MarshalAs(UnmanagedType.LPWStr)]
         string pInstanceName,
         int dwFlags);

     [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
     public delegate int LocalDBFormatMessage(
         int hrLocalDB,
         int dwFlags,
         int dwLanguageId,
         [MarshalAs(UnmanagedType.LPWStr), Out]
         StringBuilder wszMessage,
         ref int lpcchMessage);

     [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
     public delegate int LocalDBGetInstanceInfo(
         [MarshalAs(UnmanagedType.LPWStr)]
         string wszInstanceName,
         ref LocalDbInstanceInfo pInstanceInfo,
         int dwInstanceInfoSize);

     [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
     public delegate int LocalDBGetInstances(
         IntPtr pInstanceNames,
         ref int lpdwNumberOfInstances);

     [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
     public delegate int LocalDBGetVersionInfo(
         [MarshalAs(UnmanagedType.LPWStr)]
         string wszVersionName,
         IntPtr pVersionInfo, int dwVersionInfoSize);

     [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
     public delegate int LocalDBGetVersions(
         IntPtr pVersion,
         ref int lpdwNumberOfVersions);


     [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
     public delegate int LocalDBStartInstance(
         [MarshalAs(UnmanagedType.LPWStr)]
         string pInstanceName,
         int dwFlags,
         [MarshalAs(UnmanagedType.LPWStr), Out]
         StringBuilder wszSqlConnection,
         ref int lpcchSqlConnection);

     [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
     public delegate int LocalDBStopInstance(
         [MarshalAs(UnmanagedType.LPWStr)]
         string pInstanceName,
         int dwFlags, int ulTimeout);
 }
﻿using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

 static class UnmanagedLocalDbApi
 {
     static IntPtr api;

     static UnmanagedLocalDbApi()
     {
         var dllName = GetLocalDbDllName();

         api = Kernel32.LoadLibraryEx(dllName, IntPtr.Zero, Kernel32.LoadLibraryFlags.LoadLibrarySearchDefaultDirs);
         if (api == IntPtr.Zero)
         {
             throw new Win32Exception();
         }

         CreateInstance = GetFunction<LocalDBCreateInstance>();
         GetInstanceInfo = GetFunction<LocalDBGetInstanceInfo>();
         GetInstances = GetFunction<LocalDBGetInstances>();
         StartInstance = GetFunction<LocalDBStartInstance>();
         StopInstance = GetFunction<LocalDBStopInstance>();
     }

     public static string ApiVersion;
     public const int MaxPath = 260;
     public const int MaxName = 129;
     public const int MaxSid = 187;

     public static LocalDBCreateInstance CreateInstance;
     public static LocalDBGetInstanceInfo GetInstanceInfo;
     public static LocalDBGetInstances GetInstances;
     public static LocalDBStartInstance StartInstance;
     public static LocalDBStopInstance StopInstance;

     static string GetLocalDbDllName()
     {
         var registryView = GetRegistryView();
         using (var rootKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, registryView))
         {
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
             using (var versionKey = versions.OpenSubKey(latest.ToString()))
             {
                 if (versionKey == null)
                 {
                     throw new InvalidOperationException("Could not find LocalDb dll.");
                 }
                 ApiVersion = latest.ToString();
                 return (string) versionKey.GetValue("InstanceAPIPath");
             }
         }
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

     static T GetFunction<T>()
         where T : class
     {
         var name = typeof(T).Name;
         var ptr = Kernel32.GetProcAddress(api, name);
         if (ptr == IntPtr.Zero)
         {
             throw new EntryPointNotFoundException(name);
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
         int dwFlags,
         int ulTimeout);
 }
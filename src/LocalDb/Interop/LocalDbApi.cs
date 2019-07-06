﻿using System;
 using System.Collections.Generic;
 using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

 static class LocalDbApi
 {
     static IntPtr api;

     static LocalDbApi()
     {
         var (path, version) = LocalDbRegistryReader.GetInfo();
         ApiVersion = version;
         api = Kernel32.LoadLibraryEx(path, IntPtr.Zero, Kernel32.LoadLibraryFlags.LoadLibrarySearchDefaultDirs);
         if (api == IntPtr.Zero)
         {
             throw new Win32Exception();
         }

         createInstance = GetFunction<LocalDBCreateInstance>();
         getInstanceInfo = GetFunction<LocalDBGetInstanceInfo>();
         getInstances = GetFunction<LocalDBGetInstances>();
         startInstance = GetFunction<LocalDBStartInstance>();
         stopInstance = GetFunction<LocalDBStopInstance>();
     }

     public static string ApiVersion;
     public const int MaxPath = 260;
     public const int MaxName = 129;
     public const int MaxSid = 187;

     public static LocalDBCreateInstance createInstance;
     public static LocalDBGetInstanceInfo getInstanceInfo;
     public static LocalDBGetInstances getInstances;
     public static LocalDBStartInstance startInstance;
     public static LocalDBStopInstance stopInstance;

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

     public static List<string> GetInstanceNames()
     {
         var count = 0;
         getInstances(IntPtr.Zero, ref count);
         var length = MaxName * sizeof(char);
         var pointer = Marshal.AllocHGlobal(count * length);
         try
         {
             getInstances(pointer, ref count);
             var names = new List<string>(count);
             for (var i = 0; i < count; i++)
             {
                 var idx = IntPtr.Add(pointer, length * i);
                 names.Add(Marshal.PtrToStringAuto(idx));
             }

             return names;
         }
         finally
         {
             Marshal.FreeHGlobal(pointer);
         }
     }

     public static LocalDbInstanceInfo GetInstance(string instanceName)
     {
         var info = new LocalDbInstanceInfo();
         getInstanceInfo(instanceName, ref info, Marshal.SizeOf(typeof(LocalDbInstanceInfo)));
         return info;
     }

     public static void CreateInstance(string instanceName)
     {
         createInstance(ApiVersion, instanceName, 0);
     }

     public static void StartInstance(string instanceName)
     {
         var connection = new StringBuilder(LocalDbApi.MaxPath);
         var size = connection.Capacity;

         startInstance(instanceName, 0, connection, ref size);
     }

     public static void StopInstance(string instanceName, TimeSpan timeout)
     {
         stopInstance(instanceName, 0, (int) timeout.TotalSeconds);
     }
 }
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

static class ManagedLocalDbApi
{
    public static List<string> GetInstanceNames()
    {
        var count = 0;
        UnmanagedLocalDbApi.GetInstances(IntPtr.Zero, ref count);
        var length = UnmanagedLocalDbApi.MaxName * sizeof(char);
        var ptr = Marshal.AllocHGlobal(count * length);
        try
        {
            UnmanagedLocalDbApi.GetInstances(ptr, ref count);
            var names = new List<string>(count);
            for (var i = 0; i < count; i++)
            {
                var idx = IntPtr.Add(ptr, length * i);
                names.Add(Marshal.PtrToStringAuto(idx));
            }

            return names;
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
    }

    public static LocalDbInstanceInfo GetInstance(string instanceName)
    {
        var info = new LocalDbInstanceInfo();
        UnmanagedLocalDbApi.GetInstanceInfo(instanceName, ref info, Marshal.SizeOf(typeof(LocalDbInstanceInfo)));
        return info;
    }

    public static void CreateInstance(string instanceName)
    {
        UnmanagedLocalDbApi.CreateInstance(UnmanagedLocalDbApi.ApiVersion, instanceName, 0);
    }

    public static void StartInstance(string instanceName)
    {
        var connection = new StringBuilder(UnmanagedLocalDbApi.MaxPath);
        var size = connection.Capacity;

        UnmanagedLocalDbApi.StartInstance(instanceName, 0, connection, ref size);
    }

    public static void StopInstance(string instanceName, TimeSpan timeout)
    {
        UnmanagedLocalDbApi.StopInstance(instanceName, 0, (int) timeout.TotalSeconds);
    }

    public static void DeleteInstance(string instanceName)
    {
        UnmanagedLocalDbApi.DeleteInstance(instanceName, 0);
    }
}
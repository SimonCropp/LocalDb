using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

public class ManagedLocalDbApi
{
    private readonly UnmanagedLocalDbApi _api = new UnmanagedLocalDbApi();

    public IList<string> GetInstanceNames()
    {
        int count = 0;
        _api.GetInstances(IntPtr.Zero, ref count);
        var length = UnmanagedLocalDbApi.MaxName*sizeof (char);
        IntPtr ptr = Marshal.AllocHGlobal(count * length);
        try
        {
            _api.GetInstances(ptr, ref count);
            var names = new List<string>(count);
            for (int i = 0; i < count; i++)
            {
                IntPtr idx = IntPtr.Add(ptr, length*i);
                names.Add(Marshal.PtrToStringAuto(idx));
            }
            return names;
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
    }

    public LocalDbInstanceInfo GetInstance(string instanceName)
    {
        LocalDbInstanceInfo info = new LocalDbInstanceInfo();
        _api.GetInstanceInfo(instanceName, ref info, Marshal.SizeOf(typeof(LocalDbInstanceInfo)));
        return info;
    }

    public void CreateInstance(string instanceName)
    {
        _api.CreateInstance(_api.ApiVersion, instanceName, 0);
    }

    public string StartInstance(string instanceName)
    {
        var connection = new StringBuilder(UnmanagedLocalDbApi.MaxPath);
        int size = connection.Capacity;

        _api.StartInstance(instanceName, 0, connection, ref size);

        string namedPipe = connection.ToString();
        return namedPipe;
    }

    public void StopInstance(string instanceName, TimeSpan timeout)
    {
        _api.StopInstance(instanceName, 0, (int)timeout.TotalSeconds);
    }
    public void DeleteInstance(string instanceName)
    {
        _api.DeleteInstance(instanceName, 0);
    }

    public void StartTracing()
    {
        _api.StartTracing();
    }

    public void StopTracing()
    {
        _api.StopTracing();
    }
}
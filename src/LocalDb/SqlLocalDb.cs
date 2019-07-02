using System;
using System.Collections.Generic;

static class SqlLocalDb
{
    static ManagedLocalDbApi managedLocalDbApi = new ManagedLocalDbApi();
    public static State Start(string instance)
    {
        var localDbInstanceInfo = managedLocalDbApi.GetInstance(instance);

        if (!localDbInstanceInfo.Exists)
        {
            managedLocalDbApi.CreateInstance(instance);
            managedLocalDbApi.StartInstance(instance);
            return State.NotExists;
        }

        if (!localDbInstanceInfo.IsRunning)
        {
            managedLocalDbApi.StartInstance(instance);
        }

        return State.Running;
    }

    public static IEnumerable<string> Instances()
    {
        return managedLocalDbApi.GetInstanceNames();
    }

    public static void DeleteInstance(string instance)
    {
        managedLocalDbApi.StopInstance(instance, TimeSpan.FromSeconds(10));
        managedLocalDbApi.DeleteInstance(instance);
    }
}
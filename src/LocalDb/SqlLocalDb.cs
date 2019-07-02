using System;

static class SqlLocalDb
{
    public static State Start(string instance)
    {
        var localDbInstanceInfo = ManagedLocalDbApi.GetInstance(instance);

        if (!localDbInstanceInfo.Exists)
        {
            ManagedLocalDbApi.CreateInstance(instance);
            ManagedLocalDbApi.StartInstance(instance);
            return State.NotExists;
        }

        if (!localDbInstanceInfo.IsRunning)
        {
            ManagedLocalDbApi.StartInstance(instance);
        }

        return State.Running;
    }

    public static void DeleteInstance(string instance)
    {
        ManagedLocalDbApi.StopInstance(instance, TimeSpan.FromSeconds(10));
        ManagedLocalDbApi.DeleteInstance(instance);
    }
}
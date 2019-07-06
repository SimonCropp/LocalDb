using System.Diagnostics;
using ObjectApproval;
using Xunit;
using Xunit.Abstractions;

public class SqlLocalDbTests :
    XunitLoggingBase
{
    [Fact]
    public void Instances()
    {
        var collection = ManagedLocalDbApi.GetInstanceNames();
        foreach (var instance in collection)
        {
            Trace.WriteLine(instance);
        }
        Assert.NotEmpty(collection);
    }

    [Fact]
    public void NonInstanceInfo()
    {
        var info = ManagedLocalDbApi.GetInstance("Missing");
        Assert.False(info.Exists);
    }

    [Fact]
    public void DeleteInstance()
    {
        SqlLocalDb.Start("DeleteInstance");
        SqlLocalDb.DeleteInstance("DeleteInstance");
        Assert.False(ManagedLocalDbApi.GetInstance("DeleteInstance").Exists);
    }

    [Fact]
    public void Info()
    {
        SqlLocalDb.Start("InfoTest");
        var info = ManagedLocalDbApi.GetInstance("InfoTest");

        ObjectApprover.VerifyWithJson(info);
        SqlLocalDb.DeleteInstance("InfoTest");
    }

    //[Fact]
    //public void DeleteAll()
    //{
    //    foreach (var instance in ManagedLocalDbApi.GetInstanceNames())
    //    {
    //        SqlLocalDb.DeleteInstance(instance);
    //    }
    //}

    public SqlLocalDbTests(ITestOutputHelper output) :
        base(output)
    {
    }
}
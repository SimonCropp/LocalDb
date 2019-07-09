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
        var collection = LocalDbApi.GetInstanceNames();
        foreach (var instance in collection)
        {
            Trace.WriteLine(instance);
        }
        Assert.NotEmpty(collection);
    }

    [Fact]
    public void NonInstanceInfo()
    {
        var info = LocalDbApi.GetInstance("Missing");
        Assert.False(info.Exists);
    }

    [Fact]
    public void DeleteInstance()
    {
        LocalDbApi.CreateAndStart("DeleteInstance");
        LocalDbApi.StopAndDelete("DeleteInstance");
        Assert.False(LocalDbApi.GetInstance("DeleteInstance").Exists);
    }

    [Fact]
    public void Info()
    {
        LocalDbApi.CreateAndStart("InfoTest");
        var info = LocalDbApi.GetInstance("InfoTest");

        ObjectApprover.VerifyWithJson(info);
        LocalDbApi.StopAndDelete("InfoTest");
    }

    //[Fact]
    //public void DeleteAll()
    //{
    //    foreach (var instance in LocalDbApi.GetInstanceNames())
    //    {
    //        Trace.WriteLine(instance);
    //        LocalDbApi.StopAndDelete(instance);
    //    }
    //}

    public SqlLocalDbTests(ITestOutputHelper output) :
        base(output)
    {
    }
}
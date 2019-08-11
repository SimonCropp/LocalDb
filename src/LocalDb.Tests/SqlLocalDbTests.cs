using System.Diagnostics;
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
    public void Info()
    {
        LocalDbApi.CreateAndStart("InfoTest");
        var info = LocalDbApi.GetInstance("InfoTest");

        ObjectApprover.Verify(info);
        LocalDbApi.StopAndDelete("InfoTest");
        Assert.False(LocalDbApi.GetInstance("InfoTest").Exists);
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
public class SqlLocalDbTests
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
    public async Task Info()
    {
        LocalDbApi.CreateAndStart("InfoTest");
        var info = LocalDbApi.GetInstance("InfoTest");

        await Verify(info);
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
}
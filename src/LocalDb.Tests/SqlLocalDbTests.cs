[TestFixture]
public class SqlLocalDbTests
{
    [Test]
    public void Instances()
    {
        var collection = LocalDbApi.GetInstanceNames();
        foreach (var instance in collection)
        {
            Trace.WriteLine(instance);
        }

        IsNotEmpty(collection);
    }

    [Test]
    public void NonInstanceInfo()
    {
        var info = LocalDbApi.GetInstance("Missing");
        False(info.Exists);
    }

    [Test]
    public async Task Info()
    {
        LocalDbApi.CreateAndStart("InfoTest");
        var info = LocalDbApi.GetInstance("InfoTest");

        await Verify(info)
            .Snapshot(
                """
                {
                  Size: 1460,
                  InstanceName: InfoTest,
                  Exists: true,
                  ConfigurationCorrupted: false,
                  IsRunning: true,
                  IsShared: false,
                  SharedInstanceName: ,
                  IsAutomatic: false
                }
                """);
        LocalDbApi.StopAndDelete("InfoTest");
        False(LocalDbApi.GetInstance("InfoTest").Exists);
    }

    //[Test]
    //public void DeleteAll()
    //{
    //    foreach (var instance in LocalDbApi.GetInstanceNames())
    //    {
    //        Trace.WriteLine(instance);
    //        LocalDbApi.StopAndDelete(instance);
    //    }
    //}
}
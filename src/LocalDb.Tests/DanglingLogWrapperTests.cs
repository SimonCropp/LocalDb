[TestFixture]
public class DanglingLogWrapperTests
{
    [Test]
    public void Run()
    {
        var name = "DanglingLogWrapperTests";
        LocalDbApi.StopAndDelete(name);
        using var instance = new Wrapper(name, DirectoryFinder.Find(name));
        instance.Start(DateTime.Now, TestDbBuilder.CreateTable);
        instance.DeleteInstance();
    }
}

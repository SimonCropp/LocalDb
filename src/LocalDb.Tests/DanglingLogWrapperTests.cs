[TestFixture]
public class DanglingLogWrapperTests
{
    [Test]
    public void Run()
    {
        var name = "DanglingLogWrapperTests";
        LocalDbApi.StopAndDelete(name);
        using var instance = new Wrapper(_ => new SqlConnection(_), name, DirectoryFinder.Find(name));
        instance.Start(DateTime.Now, TestDbBuilder.CreateTable);
        instance.DeleteInstance();
    }
}

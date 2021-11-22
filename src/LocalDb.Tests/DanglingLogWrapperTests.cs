using Xunit;

public class DanglingLogWrapperTests
{
    [Fact]
    public void Run()
    {
        var name = "DanglingLogWrapperTests";
        LocalDbApi.StopAndDelete(name);
        Wrapper instance = new(s => new(s), name, DirectoryFinder.Find(name));
        instance.Start(DateTime.Now, TestDbBuilder.CreateTable);
    }
}
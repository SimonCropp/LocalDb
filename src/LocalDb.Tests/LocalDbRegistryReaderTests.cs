using Xunit;

public class LocalDbRegistryReaderTests
{
    [Fact]
    public void GetInfo()
    {
        var info = LocalDbRegistryReader.GetInfo();
        Assert.NotNull(info.path);
        Assert.NotNull(info.version);
    }
}
using VerifyXunit;
using Xunit;
using Xunit.Abstractions;

public class LocalDbRegistryReaderTests :
    VerifyBase
{
    [Fact]
    public void GetInfo()
    {
        var info = LocalDbRegistryReader.GetInfo();
        Assert.NotNull(info.path);
        Assert.NotNull(info.version);
        WriteLine(info.path);
        WriteLine(info.version);
    }

    public LocalDbRegistryReaderTests(ITestOutputHelper output) :
        base(output)
    {
    }
}
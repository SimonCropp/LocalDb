public class LocalDbRegistryReaderTests
{
    [Test]
    public void GetInfo()
    {
        var info = LocalDbRegistryReader.GetInfo();
        NotNull(info.path);
        NotNull(info.version);
    }
}
[SetUpFixture]
public class AssemblySetup
{
    [OneTimeTearDown]
    public void Cleanup()
    {
        LocalDbTestBase<TheDbContext>.Shutdown();
        LocalDbTestBase<DefaultTimestampDbContext>.Shutdown();
        LocalDbTestBase<TimestampDbContext>.Shutdown();
    }
}

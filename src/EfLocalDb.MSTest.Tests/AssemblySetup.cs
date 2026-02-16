[TestClass]
public class AssemblySetup
{
    [AssemblyCleanup]
    public static void Cleanup()
    {
        LocalDbTestBase<TheDbContext>.Shutdown();
        LocalDbTestBase<DefaultTimestampDbContext>.Shutdown();
        LocalDbTestBase<TimestampDbContext>.Shutdown();
    }
}

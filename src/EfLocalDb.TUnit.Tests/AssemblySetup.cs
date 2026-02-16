public static class AssemblySetup
{
    [After(HookType.Assembly)]
    public static void Cleanup()
    {
        LocalDbTestBase<TheDbContext>.Shutdown();
        LocalDbTestBase<DefaultTimestampDbContext>.Shutdown();
        LocalDbTestBase<TimestampDbContext>.Shutdown();
    }
}

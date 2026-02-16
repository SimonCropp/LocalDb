[assembly: AssemblyFixture(typeof(AssemblySetup))]

public class AssemblySetup : IAsyncDisposable
{
    public ValueTask DisposeAsync()
    {
        LocalDbTestBase<TheDbContext>.Shutdown();
        LocalDbTestBase<DefaultTimestampDbContext>.Shutdown();
        LocalDbTestBase<TimestampDbContext>.Shutdown();
        return ValueTask.CompletedTask;
    }
}

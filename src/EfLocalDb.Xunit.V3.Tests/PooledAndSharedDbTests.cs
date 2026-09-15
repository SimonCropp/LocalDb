public class PooledAndSharedDbTests : LocalDbTestBase<TheDbContext>
{
    // InitializeAsync would throw before the test body runs, so skip it here and
    // invoke the base implementation from within the test.
    public override ValueTask InitializeAsync() => ValueTask.CompletedTask;

    [Fact]
    [PooledDb]
    [SharedDb]
    public async Task Throws()
    {
        var exception = await Assert.ThrowsAsync<Exception>(() => base.InitializeAsync().AsTask());
        Assert.Equal("[PooledDb], [SharedDb] and [NewDb] are mutually exclusive. Use only one on a test method.", exception.Message);
    }
}

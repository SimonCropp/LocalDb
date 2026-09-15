public class PooledAndSharedDbTests : LocalDbTestBase<TheDbContext>
{
    // SetUp would throw before the test body runs, so skip it here and
    // invoke the base implementation from within the test.
    public override Task SetUp() => Task.CompletedTask;

    [Test]
    [PooledDb]
    [SharedDb]
    public async Task Throws()
    {
        var exception = (await Assert.ThrowsExactlyAsync<Exception>(() => base.SetUp()))!;
        await Assert.That(exception.Message)
            .IsEqualTo("[PooledDb], [SharedDb] and [NewDb] are mutually exclusive. Use only one on a test method.");
    }
}

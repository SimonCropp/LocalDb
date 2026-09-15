[TestFixture]
public class PooledAndSharedDbTests :
    LocalDbTestBase<TheDbContext>
{
    // SetUp would throw before the test body runs, so skip it here and
    // invoke the base implementation from within the test.
    public override Task SetUp() => Task.CompletedTask;

    [Test]
    [PooledDb]
    [SharedDb]
    public void Throws()
    {
        var exception = ThrowsAsync<Exception>(() => base.SetUp())!;
        AreEqual("[PooledDb], [SharedDb] and [NewDb] are mutually exclusive. Use only one on a test method.", exception.Message);
    }
}

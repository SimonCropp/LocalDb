[PooledDb]
public class ClassLevelPooledDbTests : LocalDbTestBase<TheDbContext>
{
    [Test]
    public async Task UsesPooledDb() =>
        await Assert.That(Database.Transaction).IsNotNull();

    [Test]
    [SharedDb]
    public async Task MethodOverridesClass() =>
        await Assert.That(Database.Name).IsEqualTo("Shared");

    [Test]
    [NewDb]
    public async Task NewDbOptsOut()
    {
        await Assert.That(Database.Transaction).IsNull();
        await Assert.That(Database.Name).IsNotEqualTo("Shared");
    }
}

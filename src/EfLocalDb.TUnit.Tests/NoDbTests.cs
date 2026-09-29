public class NoDbTests : LocalDbTestBase<TheDbContext>
{
    [Test]
    [NoDb]
    public async Task HasNoDatabase() =>
        await Assert.That(Database).IsNull();

    [Test]
    [NoDb]
    public async Task ArrangeDataThrows()
    {
        var exception = await Assert.That(() => _ = ArrangeData).Throws<Exception>();
        await Assert.That(exception!.Message).Contains("[NoDb]");
    }

    [Test]
    [NoDb]
    public async Task IsRecording() =>
        await Assert.That(Recording.IsRecording()).IsTrue();

    [Test]
    [NoDb]
    public async Task ResetThrows() =>
        await Assert.That(Reset).Throws<Exception>();

    [Test]
    public async Task UnmarkedMethodHasDatabase() =>
        await Assert.That(Database).IsNotNull();
}

[NoDb]
public class ClassLevelNoDbTests : LocalDbTestBase<TheDbContext>
{
    [Test]
    public async Task HasNoDatabase() =>
        await Assert.That(Database).IsNull();

    [Test]
    [PooledDb]
    public async Task MethodOverridesClass() =>
        await Assert.That(Database.Transaction).IsNotNull();
}

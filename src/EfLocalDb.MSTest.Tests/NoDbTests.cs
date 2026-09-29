[TestClass]
public class NoDbTests : LocalDbTestBase<TheDbContext>
{
    [TestMethod]
    [NoDb]
    public void HasNoDatabase() =>
        Assert.IsNull(Database);

    [TestMethod]
    [NoDb]
    public void ArrangeDataThrows()
    {
        var exception = Assert.ThrowsExactly<Exception>(() => _ = ArrangeData);
        Assert.Contains("[NoDb]", exception.Message);
    }

    [TestMethod]
    [NoDb]
    public void IsRecording() =>
        Assert.IsTrue(Recording.IsRecording());

    [TestMethod]
    [NoDb]
    public async Task ResetThrows() =>
        await Assert.ThrowsExactlyAsync<Exception>(Reset);

    [TestMethod]
    public void UnmarkedMethodHasDatabase() =>
        Assert.IsNotNull(Database);
}

[TestClass]
[NoDb]
public class ClassLevelNoDbTests : LocalDbTestBase<TheDbContext>
{
    [TestMethod]
    public void HasNoDatabase() =>
        Assert.IsNull(Database);

    [TestMethod]
    [PooledDb]
    public void MethodOverridesClass() =>
        Assert.IsNotNull(Database.Transaction);
}

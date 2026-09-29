public class NoDbTests : LocalDbTestBase<TheDbContext>
{
    [Fact]
    [NoDb]
    public void HasNoDatabase() =>
        Assert.Null(Database);

    [Fact]
    [NoDb]
    public void ArrangeDataThrows()
    {
        var exception = Assert.Throws<Exception>(() => _ = ArrangeData);
        Assert.Contains("[NoDb]", exception.Message);
    }

    [Fact]
    [NoDb]
    public void IsRecording() =>
        Assert.True(Recording.IsRecording());

    [Fact]
    [NoDb]
    public Task ResetThrows() =>
        Assert.ThrowsAsync<Exception>(Reset);

    [Fact]
    public void UnmarkedMethodHasDatabase() =>
        Assert.NotNull(Database);
}

[NoDb]
public class ClassLevelNoDbTests : LocalDbTestBase<TheDbContext>
{
    [Fact]
    public void HasNoDatabase() =>
        Assert.Null(Database);

    [Fact]
    [PooledDb]
    public void MethodOverridesClass() =>
        Assert.NotNull(Database.Transaction);
}

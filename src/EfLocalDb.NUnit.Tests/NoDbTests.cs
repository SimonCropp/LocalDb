[TestFixture]
public class NoDbTests :
    LocalDbTestBase<TheDbContext>
{
    [Test]
    [NoDb]
    public void HasNoDatabase() =>
        IsNull(Database);

    [Test]
    [NoDb]
    public void ArrangeDataIsNull() =>
        IsNull(ArrangeData);

    [Test]
    [NoDb]
    public void ActDataThrows()
    {
        var exception = Throws<Exception>(() => _ = ActData)!;
        That(exception.Message, Does.Contain("[NoDb]"));
    }

    [Test]
    [NoDb]
    public void IsRecording() =>
        That(Recording.IsRecording(), Is.True);

    [Test]
    [NoDb]
    public void ResetThrows() =>
        ThrowsAsync<Exception>(Reset);

    [Test]
    public void UnmarkedMethodHasDatabase() =>
        IsNotNull(Database);
}

[TestFixture]
[NoDb]
public class ClassLevelNoDbTests :
    LocalDbTestBase<TheDbContext>
{
    [Test]
    public void HasNoDatabase() =>
        IsNull(Database);

    [Test]
    [PooledDb]
    public void MethodOverridesClass() =>
        IsNotNull(Database.Transaction);
}

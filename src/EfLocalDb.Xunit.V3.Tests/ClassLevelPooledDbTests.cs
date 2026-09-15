[PooledDb]
public class ClassLevelPooledDbTests : LocalDbTestBase<TheDbContext>
{
    [Fact]
    public void UsesPooledDb() =>
        Assert.NotNull(Database.Transaction);

    [Fact]
    [SharedDb]
    public void MethodOverridesClass() =>
        Assert.Equal("Shared", Database.Name);

    [Fact]
    [NewDb]
    public void NewDbOptsOut()
    {
        Assert.Null(Database.Transaction);
        Assert.NotEqual("Shared", Database.Name);
    }
}

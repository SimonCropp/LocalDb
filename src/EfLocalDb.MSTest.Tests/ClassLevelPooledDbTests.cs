[TestClass]
[PooledDb]
public class ClassLevelPooledDbTests : LocalDbTestBase<TheDbContext>
{
    [TestMethod]
    public void UsesPooledDb() =>
        Assert.IsNotNull(Database.Transaction);

    [TestMethod]
    [SharedDb]
    public void MethodOverridesClass() =>
        Assert.AreEqual("Shared", Database.Name);

    [TestMethod]
    [NewDb]
    public void NewDbOptsOut()
    {
        Assert.IsNull(Database.Transaction);
        Assert.AreNotEqual("Shared", Database.Name);
    }
}

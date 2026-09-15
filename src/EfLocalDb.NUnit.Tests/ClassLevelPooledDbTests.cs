[TestFixture]
[PooledDb]
public class ClassLevelPooledDbTests :
    LocalDbTestBase<TheDbContext>
{
    [Test]
    public void UsesPooledDb() =>
        IsNotNull(Database.Transaction);

    [Test]
    [SharedDb]
    public void MethodOverridesClass() =>
        AreEqual("Shared", Database.Name);

    [Test]
    [NewDb]
    public void NewDbOptsOut()
    {
        IsNull(Database.Transaction);
        AreNotEqual("Shared", Database.Name);
    }
}

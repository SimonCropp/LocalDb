[TestFixture]
public class DbAutoOfflineSnippetTests
{
    #region DbAutoOfflineUsageEfClassic

    static SqlInstance<MyDbContext> sqlInstance = new(
        constructInstance: connection => new(connection),
        storage: Storage.FromSuffix<MyDbContext>("DbAutoOfflineSnippetTests"),
        dbAutoOffline: true);

    #endregion

    [Test]
    public async Task TheTest()
    {
        using var database = await sqlInstance.Build();

        using (var data = database.NewDbContext())
        {
            var entity = new TheEntity
            {
                Property = "prop"
            };
            data.TestEntities.Add(entity);
            await data.SaveChangesAsync();
        }

        using (var data = database.NewDbContext())
        {
            AreEqual(1, data.TestEntities.Count());
        }
    }

    [OneTimeTearDown]
    public void Cleanup()
    {
        sqlInstance.Cleanup();
        sqlInstance.Dispose();
    }
}

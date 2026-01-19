public class DbAutoOfflineSnippetTests
{
    public class MyDbContext(DbContextOptions options) :
        DbContext(options)
    {
        public DbSet<TheEntity> TestEntities { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder model) =>
            model.Entity<TheEntity>();
    }

    #region DbAutoOfflineUsageEfCore

    static SqlInstance<MyDbContext> sqlInstance = new(
        constructInstance: builder => new(builder.Options),
        dbAutoOffline: true);

    #endregion

    [Test]
    public async Task TheTest()
    {
        await using var database = await sqlInstance.Build();

        var entity = new TheEntity
        {
            Property = "prop"
        };
        database.Context.Add(entity);
        await database.Context.SaveChangesAsync();

        AreEqual(1, database.Context.TestEntities.Count());
    }

    [OneTimeTearDown]
    public void Cleanup()
    {
        sqlInstance.Cleanup();
        sqlInstance.Dispose();
    }
}

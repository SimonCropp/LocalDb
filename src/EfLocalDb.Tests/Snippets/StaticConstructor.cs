public class StaticConstructor
{
    public class TheDbContext(DbContextOptions options) :
        DbContext(options)
    {
        public DbSet<TheEntity> TestEntities { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder model) => model.Entity<TheEntity>();
    }

    #region EfStaticConstructor

    [TestFixture]
    public class Tests
    {
        static SqlInstance<TheDbContext> sqlInstance;

        static Tests() =>
            sqlInstance = new(builder => new(builder.Options));

        [Test]
        public async Task Test()
        {
            var entity = new TheEntity
            {
                Property = "prop"
            };
            await using var database = await sqlInstance.Build([entity]);
            AreEqual(1, database.Context.TestEntities.Count());
        }

        #endregion

        [OneTimeTearDown]
        public void Cleanup()
        {
            sqlInstance.Cleanup();
            sqlInstance.Dispose();
        }
    }
}
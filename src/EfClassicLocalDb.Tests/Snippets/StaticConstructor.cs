public class EfClassicStaticConstructor
{
    public class TheDbContext(SqlConnection connection) :
        DbContext(connection, false)
    {
        public DbSet<TheEntity> TestEntities { get; set; } = null!;

        protected override void OnModelCreating(DbModelBuilder model) => model.Entity<TheEntity>();
    }
    #region EfClassicStaticConstructor

    public class Tests
    {
        static SqlInstance<TheDbContext> sqlInstance;

        static Tests() =>
            sqlInstance = new(
                connection => new(connection));

        [Test]
        public async Task Test()
        {
            var entity = new TheEntity
            {
                Property = "prop"
            };
            using var database = await sqlInstance.Build([entity]);
            AreEqual(1, database.Context.TestEntities.Count());
        }
    }

    #endregion
}
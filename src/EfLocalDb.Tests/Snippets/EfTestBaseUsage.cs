public class EfTestBase
{
    public class TheDbContext(DbContextOptions options) :
        DbContext(options)
    {
        public DbSet<TheEntity> TestEntities { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder model) => model.Entity<TheEntity>();
    }

    #region EfTestBase

    public abstract class TestBase
    {
        static SqlInstance<TheDbContext> sqlInstance;

        static TestBase() =>
            sqlInstance = new(
                constructInstance: builder => new(builder.Options));

        public static Task<SqlDatabase<TheDbContext>> LocalDb(
            [CallerFilePath] string testFile = "",
            string? databaseSuffix = null,
            [CallerMemberName] string memberName = "") =>
            sqlInstance.Build(testFile, databaseSuffix, memberName);
    }

    public class Tests :
        TestBase
    {
        [Test]
        public async Task Test()
        {
            await using var database = await LocalDb();
            var entity = new TheEntity
            {
                Property = "prop"
            };
            await database.AddData(entity);

            AreEqual(1, database.Context.TestEntities.Count());
        }
    }

    #endregion
}
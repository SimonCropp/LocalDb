public class EfClassicTestBase
{
    public class TheDbContext(SqlConnection connection) :
        DbContext(connection, false)
    {
        public DbSet<TheEntity> TestEntities { get; set; } = null!;

        protected override void OnModelCreating(DbModelBuilder model) => model.Entity<TheEntity>();
    }

    #region EfClassicTestBase

    [TestFixture]
    public abstract class TestBase
    {
        static SqlInstance<TheDbContext> sqlInstance;

        static TestBase() =>
            sqlInstance = new(
                constructInstance: connection => new(connection));

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
            using var database = await LocalDb();
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
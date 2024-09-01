public class EfSnippetTests
{
    public class MyDbContext(DbContextOptions options) :
        DbContext(options)
    {
        public DbSet<TheEntity> TestEntities { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder model) => model.Entity<TheEntity>();
    }

    static SqlInstance<MyDbContext> sqlInstance;

    static EfSnippetTests() =>
        sqlInstance = new(
            builder => new(builder.Options));

    #region EfTest

    [Test]
    public async Task TheTest()
    {
        #region EfBuildDatabase

        await using var database = await sqlInstance.Build();

        #endregion

        #region EfBuildContext

        await using (var data = database.NewDbContext())
        {

            #endregion

            var entity = new TheEntity
            {
                Property = "prop"
            };
            data.Add(entity);
            await data.SaveChangesAsync();
        }

        await using (var data = database.NewDbContext())
        {
            AreEqual(1, data.TestEntities.Count());
        }

        #endregion
    }

    [Test]
    public async Task TheTestWithDbName()
    {
        #region EfWithDbName

        await using var database = await sqlInstance.Build("TheTestWithDbName");

        #endregion

        var entity = new TheEntity
        {
            Property = "prop"
        };
        await database.AddData(entity);

        AreEqual(1, database.Context.TestEntities.Count());
    }
}
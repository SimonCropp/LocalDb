public class EfBuildTemplate
{
    public class TheDbContext(DbContextOptions options) :
        DbContext(options)
    {
        public DbSet<TheEntity> TestEntities { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder model) => model.Entity<TheEntity>();
    }

    #region EfBuildTemplate

    public class BuildTemplate
    {
        static SqlInstance<TheDbContext> sqlInstance;

        static BuildTemplate() =>
            sqlInstance = new(
                constructInstance: builder => new(builder.Options),
                buildTemplate: async context =>
                {
                    await context.Database.EnsureCreatedAsync();
                    var entity = new TheEntity
                    {
                        Property = "prop"
                    };
                    context.Add(entity);
                    await context.SaveChangesAsync();
                });

        [Test]
        public async Task BuildTemplateTest()
        {
            await using var database = await sqlInstance.Build();

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
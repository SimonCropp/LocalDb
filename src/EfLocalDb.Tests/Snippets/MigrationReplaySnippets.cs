// ReSharper disable UnusedParameter.Local

public class MigrationReplaySnippets
{
    #region MigrationReplayInstance

    // the template is left empty, so each test migrates forward from nothing
    static SqlInstance<MyDbContext> sqlInstance = new(
        constructInstance: builder => new(builder.Options),
        buildTemplate: _ => Task.CompletedTask);

    #endregion

    public static async Task Usage()
    {
        #region MigrationReplayUsage

        await using var database = await sqlInstance.Build();

        await database.Context.ReplayRecentMigrations(
            count: 5,
            afterEachMigration: ApplyDeploymentState);

        #endregion
    }

    #region MigrationReplayAfterEach

    // whatever the deployment does after migrating: enabling
    // change tracking, rebuilding views, re-granting permissions
    static Task ApplyDeploymentState(MyDbContext data) =>
        Task.CompletedTask;

    #endregion

    public class MyDbContext(DbContextOptions options) :
        DbContext(options)
    {
        public DbSet<TheEntity> TestEntities { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder model) =>
            model.Entity<TheEntity>();
    }
}

// ReSharper disable UnusedParameter.Local

public class TestSpecificMigration
{
    #region MigrateToSpecificTarget

    static SqlInstance<MyDbContext> sqlInstance = new(
        buildTemplate: async (connection, options) =>
        {
            await using var data = new MyDbContext(options.Options);
            var migrator = data.GetInfrastructure()
                .GetRequiredService<IMigrator>();
            // apply up to and including a target migration
            await migrator.MigrateAsync("Migration_002_AddOrders");
        },
        constructInstance: builder => new(builder.Options));

    #endregion

    #region TestSingleMigration

    [Test]
    public async Task TestNextMigration()
    {
        await using var database = await sqlInstance.Build();

        // apply the next migration under test
        var migrator = database.Context
            .GetInfrastructure()
            .GetRequiredService<IMigrator>();
        await migrator.MigrateAsync("Migration_003_AddOrderStatus");

        // verify the migration applied the expected schema change
        await using var command = database.Connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_NAME = 'Orders'
              AND COLUMN_NAME = 'Status'
            """;
        var result = (int) (await command.ExecuteScalarAsync())!;
        That(result, Is.EqualTo(1));
    }

    #endregion

    class MyDbContext(DbContextOptions options) :
        DbContext(options)
    {
        public DbSet<TheEntity> TestEntities { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder model) =>
            model.Entity<TheEntity>();
    }
}

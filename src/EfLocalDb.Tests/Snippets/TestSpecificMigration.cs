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
            await migrator.MigrateAsync("AddOrders");
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
        await migrator.MigrateAsync("AddOrderStatus");

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

    public class MyDbContext(DbContextOptions options) :
        DbContext(options)
    {
        public DbSet<TheEntity> TestEntities { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder model) =>
            model.Entity<TheEntity>();
    }
}

[DbContext(typeof(TestSpecificMigration.MyDbContext))]
[Migration("20260101000001_InitialCreate")]
public class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder builder) =>
        builder.CreateTable(
            name: "TestEntities",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Property = table.Column<string>(nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_TestEntities", x => x.Id));

    protected override void Down(MigrationBuilder builder) =>
        builder.DropTable("TestEntities");
}

[DbContext(typeof(TestSpecificMigration.MyDbContext))]
[Migration("20260101000002_AddOrders")]
public class AddOrders : Migration
{
    protected override void Up(MigrationBuilder builder) =>
        builder.CreateTable(
            name: "Orders",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Description = table.Column<string>(nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_Orders", x => x.Id));

    protected override void Down(MigrationBuilder builder) =>
        builder.DropTable("Orders");
}

[DbContext(typeof(TestSpecificMigration.MyDbContext))]
[Migration("20260101000003_AddOrderStatus")]
public class AddOrderStatus : Migration
{
    protected override void Up(MigrationBuilder builder) =>
        builder.AddColumn<int>(
            name: "Status",
            table: "Orders",
            nullable: false,
            defaultValue: 0);

    protected override void Down(MigrationBuilder builder) =>
        builder.DropColumn(name: "Status", table: "Orders");
}

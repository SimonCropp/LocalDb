public class ReplayDbContext(DbContextOptions options) :
    DbContext(options)
{
    public DbSet<ReplayEntity> Entities { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder model) =>
        model.Entity<ReplayEntity>();
}

public class ReplayEntity
{
    public int Id { get; set; }
    public string? Property { get; set; }
}

[DbContext(typeof(ReplayDbContext))]
[Migration("20260101000001_ReplayInitialCreate")]
public class ReplayInitialCreate : Migration
{
    protected override void Up(MigrationBuilder builder) =>
        builder.CreateTable(
            name: "Entities",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Property = table.Column<string>(nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_Entities", x => x.Id));

    protected override void Down(MigrationBuilder builder) =>
        builder.DropTable("Entities");
}

[DbContext(typeof(ReplayDbContext))]
[Migration("20260101000002_ReplayAddOrders")]
public class ReplayAddOrders : Migration
{
    protected override void Up(MigrationBuilder builder) =>
        builder.CreateTable(
            name: "ReplayOrders",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1")
            },
            constraints: table => table.PrimaryKey("PK_ReplayOrders", x => x.Id));

    protected override void Down(MigrationBuilder builder) =>
        builder.DropTable("ReplayOrders");
}

[DbContext(typeof(ReplayDbContext))]
[Migration("20260101000003_ReplayAddOrderStatus")]
public class ReplayAddOrderStatus : Migration
{
    protected override void Up(MigrationBuilder builder) =>
        builder.AddColumn<int>(
            name: "Status",
            table: "ReplayOrders",
            nullable: false,
            defaultValue: 0);

    protected override void Down(MigrationBuilder builder) =>
        builder.DropColumn(name: "Status", table: "ReplayOrders");
}

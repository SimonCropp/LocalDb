public class TrackedDbContext(DbContextOptions options) :
    DbContext(options)
{
    public DbSet<TrackedEntity> Tracked { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder model) =>
        model.Entity<TrackedEntity>();
}

public class TrackedEntity
{
    public int Id { get; set; }
}

[DbContext(typeof(TrackedDbContext))]
[Migration("20260101000001_TrackedInitialCreate")]
public class TrackedInitialCreate : Migration
{
    protected override void Up(MigrationBuilder builder) =>
        builder.CreateTable(
            name: "Unrelated",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Unrelated", x => x.Id));

    protected override void Down(MigrationBuilder builder) =>
        builder.DropTable("Unrelated");
}

// Creates the table inside the replay window. The table therefore does not exist when the window
// starts, which is what makes this the interesting case: only a replay that re-applies deployment
// state between migrations will have tracked it before the next migration alters it.
[DbContext(typeof(TrackedDbContext))]
[Migration("20260101000002_TrackedCreateTable")]
public class TrackedCreateTable : Migration
{
    protected override void Up(MigrationBuilder builder) =>
        builder.CreateTable(
            name: "Tracked",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Tracked", x => x.Id));

    protected override void Down(MigrationBuilder builder) =>
        builder.DropTable("Tracked");
}

// SQL Server refuses this while change tracking is on, because change tracking requires a key
[DbContext(typeof(TrackedDbContext))]
[Migration("20260101000003_TrackedSwapKey")]
public class TrackedSwapKey : Migration
{
    protected override void Up(MigrationBuilder builder)
    {
        builder.DropPrimaryKey("PK_Tracked", "Tracked");
        builder.AddPrimaryKey("PK_Tracked", "Tracked", "Id");
    }

    protected override void Down(MigrationBuilder builder)
    {
        builder.DropPrimaryKey("PK_Tracked", "Tracked");
        builder.AddPrimaryKey("PK_Tracked", "Tracked", "Id");
    }
}

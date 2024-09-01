public class TheDbContext(DbContextOptions options) :
    DbContext(options)
{
    public DbSet<TheEntity> TestEntities { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder model) => model.Entity<TheEntity>();
}
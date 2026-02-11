public class PendingChangesDbContext(DbContextOptions options) :
    DbContext(options)
{
    public DbSet<PendingChangesEntity> PendingChangesEntities { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder model) =>
        model.Entity<PendingChangesEntity>();
}
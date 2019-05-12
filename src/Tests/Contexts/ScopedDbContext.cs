using Microsoft.EntityFrameworkCore;

public class ScopedDbContext :
    DbContext
{
    public DbSet<TestEntity> TestEntities { get; set; }

    public ScopedDbContext(DbContextOptions options) :
        base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<TestEntity>();
    }
}
using Microsoft.EntityFrameworkCore;

public class WithRebuildDbContext :
    DbContext
{
    public DbSet<TestEntity> TestEntities { get; set; } = null!;

    public WithRebuildDbContext(DbContextOptions options) :
        base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<TestEntity>();
    }
}
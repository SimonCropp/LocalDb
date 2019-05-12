using Microsoft.EntityFrameworkCore;

public class ScopedDataContext : DbContext
{
    public DbSet<TestEntity> TestEntities { get; set; }

    public ScopedDataContext(DbContextOptions options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestEntity>();
    }
}
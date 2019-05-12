using Microsoft.EntityFrameworkCore;

public class SecondaryDataContext : DbContext
{
    public DbSet<TestEntity> TestEntities { get; set; }

    public SecondaryDataContext(DbContextOptions options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestEntity>();
    }
}
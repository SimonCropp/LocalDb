using Microsoft.EntityFrameworkCore;

public class TheDataContext : DbContext
{
    public DbSet<TestEntity> TestEntities { get; set; }

    public TheDataContext(DbContextOptions options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestEntity>();
    }
}
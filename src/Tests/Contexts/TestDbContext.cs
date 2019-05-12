using Microsoft.EntityFrameworkCore;

public class TestDbContext : 
    DbContext
{
    public DbSet<TestEntity> TestEntities { get; set; }

    public TestDbContext(DbContextOptions options) : 
        base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<TestEntity>();
    }
}
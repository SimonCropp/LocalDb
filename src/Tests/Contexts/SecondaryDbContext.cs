using Microsoft.EntityFrameworkCore;

public class SecondaryDbContext :
    DbContext
{
    public DbSet<TestEntity> TestEntities { get; set; }

    public SecondaryDbContext(DbContextOptions options) :
        base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<TestEntity>();
    }
}
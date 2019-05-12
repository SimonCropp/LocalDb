using Microsoft.EntityFrameworkCore;

public class TheDbContext :
    DbContext
{
    public DbSet<TestEntity> TestEntities { get; set; }

    public TheDbContext(DbContextOptions options) :
        base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<TestEntity>();
    }
}
using Microsoft.EntityFrameworkCore;

public class DuplicateDbContext :
    DbContext
{
    public DbSet<TestEntity> TestEntities { get; set; }

    public DuplicateDbContext (DbContextOptions options) :
        base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<TestEntity>();
    }
}
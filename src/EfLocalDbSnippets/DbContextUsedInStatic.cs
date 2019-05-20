using Microsoft.EntityFrameworkCore;

public class DbContextUsedInStatic :
    DbContext
{
    public DbSet<TheEntity> TestEntities { get; set; }

    public DbContextUsedInStatic(DbContextOptions options) :
        base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<TheEntity>();
    }
}
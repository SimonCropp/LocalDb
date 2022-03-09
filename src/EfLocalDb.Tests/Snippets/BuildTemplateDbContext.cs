using Microsoft.EntityFrameworkCore;

public class BuildTemplateDbContext :
    DbContext
{
    public DbSet<TheEntity> TestEntities { get; set; } = null!;

    public BuildTemplateDbContext(DbContextOptions options) :
        base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder model) => model.Entity<TheEntity>();
}
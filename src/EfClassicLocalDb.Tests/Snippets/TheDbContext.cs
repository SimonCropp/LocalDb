using System.Data.Entity;

public class TheDbContext :
    DbContext
{
    public DbSet<TheEntity> TestEntities { get; set; } = null!;

    public TheDbContext(DbConnection connection) :
        base(connection, false)
    {
    }

    protected override void OnModelCreating(DbModelBuilder model) => model.Entity<TheEntity>();
}
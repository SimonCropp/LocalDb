using System.Data.Entity;

public class MyDbContext(DbConnection connection) :
    DbContext(connection, false)
{
    public DbSet<TheEntity> TestEntities { get; set; } = null!;

    protected override void OnModelCreating(DbModelBuilder model) => model.Entity<TheEntity>();
}
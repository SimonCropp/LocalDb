public class DuplicateDbContext(DbConnection connection) :
    DbContext(connection, false)
{
    public DbSet<TestEntity> TestEntities { get; set; } = null!;

    protected override void OnModelCreating(DbModelBuilder model) => model.Entity<TestEntity>();
}
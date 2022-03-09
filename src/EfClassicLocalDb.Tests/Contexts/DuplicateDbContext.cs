using System.Data.Common;
using System.Data.Entity;

public class DuplicateDbContext :
    DbContext
{
    public DbSet<TestEntity> TestEntities { get; set; } = null!;

    public DuplicateDbContext(DbConnection connection) :
        base(connection, false)
    {
    }

    protected override void OnModelCreating(DbModelBuilder model) => model.Entity<TestEntity>();
}
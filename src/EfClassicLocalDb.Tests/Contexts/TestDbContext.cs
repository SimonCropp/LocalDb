using System.Data.Common;
using System.Data.Entity;

public class TestDbContext :
    DbContext
{
    public DbSet<TestEntity> TestEntities { get; set; } = null!;

    public TestDbContext(DbConnection connection) :
        base(connection, false)
    {
    }

    protected override void OnModelCreating(DbModelBuilder model) => model.Entity<TestEntity>();
}
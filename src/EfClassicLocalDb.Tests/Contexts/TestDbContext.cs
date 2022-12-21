using System.Data.Entity;
using EfLocalDb;


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

#region QuietDbConfiguration
public class DbConfiguration :
    QuietDbConfiguration
{
}
#endregion
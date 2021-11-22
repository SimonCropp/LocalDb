using System.Data.Entity;
using Microsoft.Data.SqlClient;

public class TestDbContext :
    DbContext
{
    public DbSet<TestEntity> TestEntities { get; set; } = null!;

    public TestDbContext(SqlConnection connection) :
        base(connection, false)
    {
    }

    protected override void OnModelCreating(DbModelBuilder model)
    {
        model.Entity<TestEntity>();
    }
}
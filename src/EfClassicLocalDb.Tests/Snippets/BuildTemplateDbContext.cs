using System.Data.Entity;
using Microsoft.Data.SqlClient;

public class BuildTemplateDbContext :
    DbContext
{
    public DbSet<TheEntity> TestEntities { get; set; } = null!;

    public BuildTemplateDbContext(SqlConnection connection) :
        base(connection, false)
    {
    }

    protected override void OnModelCreating(DbModelBuilder model)
    {
        model.Entity<TheEntity>();
    }
}
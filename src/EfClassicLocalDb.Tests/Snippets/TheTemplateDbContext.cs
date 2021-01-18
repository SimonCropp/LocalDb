using System.Data.Common;
using System.Data.Entity;

public class TheTemplateDbContext :
    DbContext
{
    public DbSet<TheEntity> TestEntities { get; set; } = null!;

    public TheTemplateDbContext(DbConnection connection) :
        base(connection, false)
    {
    }

    protected override void OnModelCreating(DbModelBuilder model)
    {
        model.Entity<TheEntity>();
    }
}
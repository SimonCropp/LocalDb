using System.ComponentModel.DataAnnotations;

public class TemporalEntity
{
    public Guid Id { get; set; }
    public string? Property { get; set; }
    [Timestamp]
    public byte[] RowVersion { get; set; } = null!;
}

public class NonTemporalEntity
{
    public Guid Id { get; set; }
}

public class TemporalDbContext(DbContextOptions options) :
    DbContext(options)
{
    public DbSet<TemporalEntity> TemporalEntities { get; set; } = null!;
    public DbSet<NonTemporalEntity> NonTemporalEntities { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<TemporalEntity>()
            .ToTable("TemporalEntities", _ => _.IsTemporal());
        model.Entity<NonTemporalEntity>();
    }
}

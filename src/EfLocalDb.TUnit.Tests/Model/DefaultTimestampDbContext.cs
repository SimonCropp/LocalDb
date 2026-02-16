public class DefaultTimestampDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<Company> Companies { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        var company = builder.Entity<Company>();
        company.HasKey(_ => _.Id);
    }
}

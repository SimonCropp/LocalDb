using Microsoft.EntityFrameworkCore.Diagnostics;

public class InversePropertyDbContext(DbContextOptions options) :
    DbContext(options)
{
    public DbSet<Employee> Employees { get; set; } = null!;
    public DbSet<Device> Devices { get; set; } = null!;

    static IModel BuildStaticModel()
    {
        var builder = new DbContextOptionsBuilder();
        builder.UseSqlServer("Fake");
        using var dbContext = new InversePropertyDbContext(builder.Options);
        return dbContext.Model;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder builder) =>
        builder.ConfigureWarnings(_ => _.Ignore(CoreEventId.RowLimitingOperationWithoutOrderByWarning));

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Device>();
        builder.Entity<Employee>()
            .HasMany(x => x.Devices)
            .WithMany(x => x.Employees)
            .UsingEntity("EmployeeDevice");
    }
}
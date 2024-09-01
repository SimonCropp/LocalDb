// ReSharper disable UnusedVariable

public class EfExplicitName
{
    public class TheDbContext(DbContextOptions options) :
        DbContext(options)
    {
        public DbSet<TheEntity> TestEntities { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder model) => model.Entity<TheEntity>();
    }

    EfExplicitName()
    {
        #region EfExplicitName

        var sqlInstance = new SqlInstance<TheDbContext>(
            constructInstance: builder => new(builder.Options),
            storage: new("theInstanceName", @"C:\LocalDb\theInstance"));

        #endregion
    }
}
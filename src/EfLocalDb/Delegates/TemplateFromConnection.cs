namespace EfLocalDb;

public delegate Task TemplateFromConnection<TDbContext>(SqlConnection connection, DbContextOptionsBuilder<TDbContext> optionsBuilder, Cancel cancel)
    where TDbContext : DbContext;
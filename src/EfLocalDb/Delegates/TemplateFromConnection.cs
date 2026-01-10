namespace EfLocalDb;

public delegate Task TemplateFromConnection<TDbContext>(SqlConnection connection, DbContextOptionsBuilder<TDbContext> optionsBuilder)
    where TDbContext : DbContext;
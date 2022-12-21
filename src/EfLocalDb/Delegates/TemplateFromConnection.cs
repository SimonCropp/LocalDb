namespace EfLocalDb;

public delegate Task TemplateFromConnection<TDbContext>(DbConnection connection, DbContextOptionsBuilder<TDbContext> optionsBuilder)
    where TDbContext : DbContext;
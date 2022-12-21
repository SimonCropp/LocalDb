namespace EfLocalDb;

public delegate TDbContext ConstructInstance<out TDbContext>(DbConnection connection)
    where TDbContext : DbContext;
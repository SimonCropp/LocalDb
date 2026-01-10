namespace EfLocalDb;

public delegate TDbContext ConstructInstance<out TDbContext>(SqlConnection connection)
    where TDbContext : DbContext;
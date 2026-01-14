namespace EfLocalDb;

public delegate Task Callback<in TDbContext>(SqlConnection connection, TDbContext context, Cancel cancel)
    where TDbContext : DbContext;
using System.Data.Entity;

namespace EfLocalDb;

public delegate Task Callback<in TDbContext>(DbConnection connection, TDbContext context)
    where TDbContext : DbContext;
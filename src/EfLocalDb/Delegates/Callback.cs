using Microsoft.EntityFrameworkCore;

namespace EfLocalDb;

public delegate Task Callback<in TDbContext>(DbConnection connection, TDbContext context)
    where TDbContext : DbContext;
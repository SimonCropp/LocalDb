using System.Data.Entity;
using Microsoft.Data.SqlClient;

namespace EfLocalDb;

public delegate Task Callback<in TDbContext>(SqlConnection connection, TDbContext context)
    where TDbContext : DbContext;
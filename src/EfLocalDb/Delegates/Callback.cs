using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace EfLocalDb;

public delegate Task Callback<in TDbContext>(SqlConnection connection, TDbContext context)
    where TDbContext : DbContext;
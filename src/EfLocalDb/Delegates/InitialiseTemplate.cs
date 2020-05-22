using System.Data.Common;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace EfLocalDb
{
    public delegate Task InitialiseTemplate<TDbContext>(DbConnection connection, DbContextOptionsBuilder<TDbContext> optionsBuilder)
        where TDbContext : DbContext;
}
using System.Data.Common;
using System.Data.Entity;
using System.Threading.Tasks;

namespace EfLocalDb
{
    public delegate Task Callback<in TDbContext>(DbConnection connection, TDbContext context)
        where TDbContext : DbContext;
}
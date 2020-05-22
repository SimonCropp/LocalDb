using System.Data.Common;
using System.Data.Entity;

namespace EfLocalDb
{
    public delegate TDbContext ConstructInstance<out TDbContext>(DbConnection connection)
        where TDbContext : DbContext;
}
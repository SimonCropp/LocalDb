using System.Data.Entity;
using System.Threading.Tasks;

namespace EfLocalDb
{
    public delegate Task TemplateFromContext<in TDbContext>(TDbContext context)
        where TDbContext : DbContext;
}
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace EfLocalDb
{
    public delegate Task TemplateFromContext<in TDbContext>(TDbContext context)
        where TDbContext : DbContext;
}
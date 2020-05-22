using Microsoft.EntityFrameworkCore;

namespace EfLocalDb
{
    public delegate TDbContext ConstructInstance<TDbContext>(DbContextOptionsBuilder<TDbContext> optionsBuilder)
        where TDbContext : DbContext;
}
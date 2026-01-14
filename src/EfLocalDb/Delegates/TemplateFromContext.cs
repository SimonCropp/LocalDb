namespace EfLocalDb;

public delegate Task TemplateFromContext<in TDbContext>(TDbContext context, Cancel cancel)
    where TDbContext : DbContext;
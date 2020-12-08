using Microsoft.EntityFrameworkCore;

static class DefaultOptionsBuilder
{
    static LogCommandInterceptor interceptor = new();

    public static DbContextOptionsBuilder<TDbContext> Build<TDbContext>()
        where TDbContext : DbContext
    {
        DbContextOptionsBuilder<TDbContext> builder = new();
        if (LocalDbLogging.SqlLoggingEnabled)
        {
            builder.AddInterceptors(interceptor);
        }
        builder.EnableSensitiveDataLogging();
        builder.EnableDetailedErrors();
        return builder;
    }
}
using Microsoft.EntityFrameworkCore;

static class DefaultOptionsBuilder
{
    static LogCommandInterceptor interceptor = new();

    public static DbContextOptionsBuilder<TDbContext> Build<TDbContext>()
        where TDbContext : DbContext
    {
        var builder = new DbContextOptionsBuilder<TDbContext>();
        if (LocalDbLogging.SqlLoggingEnabled)
        {
            builder.AddInterceptors(interceptor);
        }

        builder.EnableSensitiveDataLogging();
        builder.EnableDetailedErrors();
        return builder;
    }
}
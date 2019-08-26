using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

static class DefaultOptionsBuilder
{
    public static DbContextOptionsBuilder<TDbContext> Build<TDbContext>()
        where TDbContext : DbContext
    {
        var builder = new DbContextOptionsBuilder<TDbContext>();
        if (LocalDbLogging.SqlLoggingEnabled)
        {
            builder.UseLoggerFactory(LoggingProvider.LoggerFactory);
        }
        builder.EnableSensitiveDataLogging();
        builder.EnableDetailedErrors();
        builder.ConfigureWarnings(warnings =>
        {
            warnings.Log(CoreEventId.IncludeIgnoredWarning);
            warnings.Log(RelationalEventId.QueryClientEvaluationWarning);
        });
        return builder;
    }
}
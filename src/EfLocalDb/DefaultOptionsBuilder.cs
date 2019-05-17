using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

static class DefaultOptionsBuilder
{
    public static DbContextOptionsBuilder<TDbContext> Build<TDbContext>()
        where TDbContext : DbContext
    {
        var builder = new DbContextOptionsBuilder<TDbContext>();
        builder.EnableSensitiveDataLogging();
        builder.EnableDetailedErrors();
        builder.ConfigureWarnings(warnings =>
        {
            warnings.Throw(CoreEventId.IncludeIgnoredWarning);
            warnings.Throw(RelationalEventId.QueryClientEvaluationWarning);
        });
        return builder;
    }
}
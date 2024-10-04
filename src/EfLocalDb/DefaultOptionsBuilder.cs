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
        builder.ReplaceService<IQueryProvider, QueryProvider>();
        builder.ReplaceService<IAsyncQueryProvider, QueryProvider>();
        builder.ReplaceService<IQueryCompilationContextFactory, QueryContextFactory>();
        builder.ReplaceService<ICompiledQueryCacheKeyGenerator, KeyGenerator>();

        builder.ConfigureWarnings(_ =>
        {
            _.Ignore(CoreEventId.SensitiveDataLoggingEnabledWarning);
            _.Default(WarningBehavior.Throw);
        });
        builder.EnableSensitiveDataLogging();
        builder.EnableDetailedErrors();
        return builder;
    }

    public static void ApplyQueryTracking<T>(this DbContextOptionsBuilder<T> builder, QueryTrackingBehavior? tracking)
        where T : DbContext
    {
        if (tracking.HasValue)
        {
            builder.UseQueryTrackingBehavior(tracking.Value);
        }
    }
}

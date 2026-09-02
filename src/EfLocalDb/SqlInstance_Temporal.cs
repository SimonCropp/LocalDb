namespace EfLocalDb;

public partial class SqlInstance<TDbContext>
    where TDbContext : DbContext
{
    // Temporal metadata is stripped from the runtime model, so reading it requires the
    // design-time model — a second full model compilation. Built lazily so instances that
    // never use the temporal APIs skip that cost at startup.
    Lazy<Dictionary<Type, TemporalSchema>> temporalSchemas;

    static Dictionary<Type, TemporalSchema> BuildTemporalSchemas(
        ConstructInstance<TDbContext> constructInstance,
        Action<SqlServerDbContextOptionsBuilder>? sqlOptionsBuilder)
    {
        var builder = DefaultOptionsBuilder.Build<TDbContext>();
        builder.UseSqlServer("Fake", sqlOptionsBuilder);
        using var context = constructInstance(builder);

        var schemas = new Dictionary<Type, TemporalSchema>();
        var designModel = context.GetService<IDesignTimeModel>().Model;
        foreach (var entityType in designModel.GetEntityTypes())
        {
            var schema = TemporalSchema.TryBuild(entityType);
            if (schema is not null)
            {
                schemas[entityType.ClrType] = schema;
            }
        }

        return schemas;
    }

    /// <summary>
    /// Re-stamps the entity's current PeriodStart and aligns the most recent history row's
    /// PeriodEnd to match. Use in tests to give consecutive saves distinct, deterministic
    /// temporal timestamps without Task.Delay. <paramref name="periodStart"/> must be greater
    /// than the row's previous PeriodStart or SQL Server rejects re-enabling system
    /// versioning due to overlapping periods.
    /// <para>The UPDATE bumps RowVersion. Caller is responsible for refreshing any tracked
    /// entity (or use the <see cref="SqlDatabase{TDbContext}.SetCurrentPeriodStart{TEntity}(TEntity, DateTime)"/>
    /// overload which reloads it).</para>
    /// </summary>
    public Task SetCurrentPeriodStart<TEntity>(TDbContext context, object id, DateTime periodStart)
        where TEntity : class =>
        ResolveSchema<TEntity>().Apply(context, id, periodStart);

    /// <summary>
    /// Sets one column on every history row for <paramref name="id"/>, leaving the current row
    /// untouched. Use in tests to reproduce a history table that a migration has left in a state
    /// a freshly built database never reaches - most usefully a NULL in a column that was dropped
    /// and re-added on the temporal pair, since SQL Server does not backfill such a column into
    /// the rows already in history.
    /// <para>The period columns cannot be set this way: rewriting those corrupts the timeline
    /// <see cref="SetCurrentPeriodStart{TEntity}(TDbContext, object, DateTime)"/> maintains.</para>
    /// </summary>
    public Task SetHistoryColumn<TEntity>(TDbContext context, object id, string propertyName, object? value)
        where TEntity : class =>
        ResolveSchema<TEntity>().SetHistoryColumn(context, id, propertyName, value);

    internal TemporalSchema ResolveSchema<TEntity>()
        where TEntity : class
    {
        if (temporalSchemas.Value.TryGetValue(typeof(TEntity), out var info))
        {
            return info;
        }

        throw new InvalidOperationException(
            $"{typeof(TEntity).Name} is not configured as a temporal table");
    }
}

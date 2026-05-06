namespace EfLocalDb;

public partial class SqlInstance<TDbContext>
    where TDbContext : DbContext
{
    // Populated by BuildModel during construction (which holds the only DbContext we
    // construct at init — keeps the single-context invariant).
    Dictionary<Type, TemporalSchema> temporalSchemas = [];

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

    internal TemporalSchema ResolveSchema<TEntity>()
        where TEntity : class
    {
        if (temporalSchemas.TryGetValue(typeof(TEntity), out var info))
        {
            return info;
        }

        throw new InvalidOperationException(
            $"{typeof(TEntity).Name} is not configured as a temporal table");
    }
}

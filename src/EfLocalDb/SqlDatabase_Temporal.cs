namespace EfLocalDb;

public partial class SqlDatabase<TDbContext>
{
    /// <summary>
    /// Re-stamps the row's current PeriodStart and aligns the most recent history row's
    /// PeriodEnd to match. Use in tests to give consecutive saves distinct, deterministic
    /// temporal timestamps without Task.Delay. <paramref name="periodStart"/> must be greater
    /// than the row's previous PeriodStart or SQL Server rejects re-enabling system
    /// versioning due to overlapping periods.
    /// <para>The UPDATE bumps RowVersion. If you have a tracked entity, prefer the
    /// <see cref="SetCurrentPeriodStart{TEntity}(TEntity, DateTime)"/> overload which
    /// reloads it for you.</para>
    /// </summary>
    public Task SetCurrentPeriodStart<TEntity>(object id, DateTime periodStart)
        where TEntity : class =>
        instance.SetCurrentPeriodStart<TEntity>(Context, id, periodStart);

    /// <summary>
    /// Convenience overload that extracts the PK from <paramref name="entity"/> and reloads
    /// it from the database afterward (so the bumped RowVersion doesn't break optimistic
    /// concurrency on the next SaveChanges).
    /// <para>If <paramref name="entity"/> is not already tracked, it will be attached as
    /// Unchanged. Reload discards any unsaved property changes on the entity.</para>
    /// </summary>
    public async Task SetCurrentPeriodStart<TEntity>(TEntity entity, DateTime periodStart)
        where TEntity : class
    {
        var schema = instance.ResolveSchema<TEntity>();
        var entry = Context.Entry(entity);
        var id = entry.Property(schema.KeyPropertyName).CurrentValue
            ?? throw new InvalidOperationException("Entity primary key value is null");
        await schema.Apply(Context, id, periodStart);
        await entry.ReloadAsync();
    }
}

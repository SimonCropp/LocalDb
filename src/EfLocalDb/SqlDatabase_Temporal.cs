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
    /// Sets one column on every history row for <paramref name="id"/>, leaving the current row
    /// untouched. Use in tests to reproduce a history table that a migration has left in a state
    /// a freshly built database never reaches - most usefully a NULL in a column that was dropped
    /// and re-added on the temporal pair, since SQL Server does not backfill such a column into
    /// the rows already in history.
    /// <para>The period columns cannot be set this way: rewriting those corrupts the timeline
    /// <see cref="SetCurrentPeriodStart{TEntity}(object, DateTime)"/> maintains.</para>
    /// </summary>
    public Task SetHistoryColumn<TEntity>(object id, string propertyName, object? value)
        where TEntity : class =>
        instance.SetHistoryColumn<TEntity>(Context, id, propertyName, value);

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

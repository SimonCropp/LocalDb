using System.Runtime.CompilerServices;

namespace EfLocalDb;

public static class TemporalExtensions
{
    /// <summary>
    /// Re-stamps the current row's PeriodStart and aligns the most recent history row's
    /// PeriodEnd to match. Discovers all SQL identifiers (table, history table, period
    /// columns, PK column) from the EF model — no raw SQL or hardcoded names required
    /// at the call site.
    /// <para>Use in tests to give consecutive saves distinct, deterministic temporal
    /// timestamps without Task.Delay. <paramref name="periodStart"/> must be greater
    /// than the row's previous PeriodStart or SQL Server rejects re-enabling system
    /// versioning due to overlapping periods.</para>
    /// <para>The UPDATE bumps RowVersion; if you have a tracked entity, prefer the
    /// <see cref="SetCurrentPeriodStart{TEntity}(DbContext, TEntity, DateTime)"/>
    /// overload which reloads it for you.</para>
    /// </summary>
    public static Task SetCurrentPeriodStart<TEntity>(
        this DbContext db,
        object id,
        DateTime periodStart)
        where TEntity : class =>
        Apply(db, TemporalSchema.For<TEntity>(db), id, periodStart);

    /// <summary>
    /// Convenience overload that extracts the PK from <paramref name="entity"/> and
    /// reloads it from the database afterward (so the bumped RowVersion doesn't break
    /// optimistic concurrency on the next SaveChanges).
    /// <para>If <paramref name="entity"/> is not already tracked, it will be attached
    /// as Unchanged. Reload discards any unsaved property changes on the entity.</para>
    /// </summary>
    public static async Task SetCurrentPeriodStart<TEntity>(
        this DbContext db,
        TEntity entity,
        DateTime periodStart)
        where TEntity : class
    {
        var info = TemporalSchema.For<TEntity>(db);
        var entry = db.Entry(entity);
        var id = entry.Property(info.KeyPropertyName).CurrentValue
            ?? throw new InvalidOperationException("Entity primary key value is null");
        await Apply(db, info, id, periodStart);
        await entry.ReloadAsync();
    }

    static async Task Apply(DbContext db, TemporalSchema info, object id, DateTime periodStart)
    {
        var qTable = $"[{info.Schema}].[{info.Table}]";
        var qHistory = $"[{info.HistorySchema}].[{info.HistoryTable}]";
        var qStart = $"[{info.PeriodStart}]";
        var qEnd = $"[{info.PeriodEnd}]";
        var qKey = $"[{info.KeyColumn}]";

        // SQL Server caches the GENERATED ALWAYS check at batch parse time, so DROP PERIOD
        // must commit in its own batch before the UPDATE — otherwise the UPDATE is rejected
        // even though PERIOD is gone by execution time.
        await Exec(db, $"ALTER TABLE {qTable} SET (SYSTEM_VERSIONING = OFF);");
        await Exec(db, $"ALTER TABLE {qTable} DROP PERIOD FOR SYSTEM_TIME;");
        try
        {
            await Exec(
                db,
                $"UPDATE {qTable} SET {qStart} = {{0}} WHERE {qKey} = {{1}};",
                periodStart, id);
            await Exec(
                db,
                $"UPDATE {qHistory} SET {qEnd} = {{0}} WHERE {qKey} = {{1}} AND {qEnd} = (SELECT MAX({qEnd}) FROM {qHistory} WHERE {qKey} = {{1}});",
                periodStart, id);
        }
        finally
        {
            await Exec(db, $"ALTER TABLE {qTable} ADD PERIOD FOR SYSTEM_TIME ({qStart}, {qEnd});");
            await Exec(db, $"ALTER TABLE {qTable} SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = {qHistory}));");
        }
    }

    // Identifiers (table/column names) come from the EF model so cannot carry user input;
    // values are passed as positional parameters via FormattableString.
    static Task Exec(DbContext db, string sql, params object[] args) =>
        db.Database.ExecuteSqlAsync(FormattableStringFactory.Create(sql, args));

    sealed record TemporalSchema(
        string Schema,
        string Table,
        string HistorySchema,
        string HistoryTable,
        string PeriodStart,
        string PeriodEnd,
        string KeyColumn,
        string KeyPropertyName)
    {
        public static TemporalSchema For<TEntity>(DbContext db)
            where TEntity : class
        {
            // Temporal metadata (history table name/schema, period property names) is
            // stripped from the runtime model — must be read from the design-time model.
            var model = db.GetService<IDesignTimeModel>().Model;
            var leaf = model.FindEntityType(typeof(TEntity))
                ?? throw new InvalidOperationException(
                    $"{typeof(TEntity).Name} is not in the EF model");

            // For TPH, temporal config lives on the root entity type that owns the table.
            var entityType = leaf.GetRootType();

            if (!entityType.IsTemporal())
            {
                throw new InvalidOperationException(
                    $"{entityType.Name} is not configured as a temporal table");
            }

            var rawSchema = entityType.GetSchema();
            var table = entityType.GetTableName()
                ?? throw new InvalidOperationException(
                    $"{entityType.Name} has no table name");
            var historySchema = entityType.GetHistoryTableSchema() ?? rawSchema ?? "dbo";
            var historyTable = entityType.GetHistoryTableName()
                ?? throw new InvalidOperationException(
                    $"{entityType.Name} has no history table name");
            var periodStart = entityType.GetPeriodStartPropertyName()
                ?? throw new InvalidOperationException(
                    $"{entityType.Name} has no PeriodStart property");
            var periodEnd = entityType.GetPeriodEndPropertyName()
                ?? throw new InvalidOperationException(
                    $"{entityType.Name} has no PeriodEnd property");

            var pk = entityType.FindPrimaryKey()
                ?? throw new InvalidOperationException(
                    $"{entityType.Name} has no primary key");
            if (pk.Properties.Count != 1)
            {
                throw new InvalidOperationException(
                    $"{entityType.Name} has a composite primary key (not supported)");
            }

            var storeObject = StoreObjectIdentifier.Table(table, rawSchema);
            var keyProperty = pk.Properties[0];
            var keyColumn = keyProperty.GetColumnName(storeObject)
                ?? throw new InvalidOperationException(
                    $"Could not resolve PK column for {entityType.Name}");

            return new(
                rawSchema ?? "dbo",
                table,
                historySchema,
                historyTable,
                periodStart,
                periodEnd,
                keyColumn,
                keyProperty.Name);
        }
    }
}

namespace EfLocalDb;

sealed record TemporalSchema(
    string OpenSql,
    string UpdateSql,
    string CloseSql,
    string KeyPropertyName,
    string VersioningOffSql,
    string VersioningOnSql,
    string HistoryTable,
    string KeyColumn,
    Dictionary<string, string> HistoryColumns)
{
    public static TemporalSchema? TryBuild(IReadOnlyEntityType entityType)
    {
        // For TPH, temporal config lives on the root entity type that owns the table.
        var root = entityType.GetRootType();
        if (!root.IsTemporal())
        {
            return null;
        }

        var rawSchema = root.GetSchema();
        var table = root.GetTableName();
        if (table is null)
        {
            return null;
        }

        var historyTable = root.GetHistoryTableName();
        var periodStart = root.GetPeriodStartPropertyName();
        var periodEnd = root.GetPeriodEndPropertyName();
        if (historyTable is null ||
            periodStart is null ||
            periodEnd is null)
        {
            return null;
        }

        var pk = root.FindPrimaryKey();
        if (pk is null || pk.Properties.Count != 1)
        {
            return null;
        }

        var storeObject = StoreObjectIdentifier.Table(table, rawSchema);
        var keyProperty = pk.Properties[0];
        var keyColumn = keyProperty.GetColumnName(storeObject);
        if (keyColumn is null)
        {
            return null;
        }

        var qTable = $"[{rawSchema ?? "dbo"}].[{table}]";
        var qHistory = $"[{root.GetHistoryTableSchema() ?? rawSchema ?? "dbo"}].[{historyTable}]";
        var qStart = $"[{periodStart}]";
        var qEnd = $"[{periodEnd}]";
        var qKey = $"[{keyColumn}]";

        // SQL Server caches the GENERATED ALWAYS check at batch parse time, so the DDL that
        // drops the PERIOD must commit in its own batch before the UPDATE — otherwise the
        // UPDATE is rejected even though PERIOD is gone by execution time. The two UPDATEs
        // and the closing DDL pair have no such cross-batch constraint and are combined.
        var openSql =
            $"""
             ALTER TABLE {qTable} SET (SYSTEM_VERSIONING = OFF);
             ALTER TABLE {qTable} DROP PERIOD FOR SYSTEM_TIME;
             """;
        var updateSql =
            $$"""
              UPDATE {{qTable}} SET {{qStart}} = {0} WHERE {{qKey}} = {1};
              UPDATE {{qHistory}} SET {{qEnd}} = {0} WHERE {{qKey}} = {1} AND {{qEnd}} = (SELECT MAX({{qEnd}}) FROM {{qHistory}} WHERE {{qKey}} = {1});
              """;
        var closeSql =
            $"""
             ALTER TABLE {qTable} ADD PERIOD FOR SYSTEM_TIME ({qStart}, {qEnd});
             ALTER TABLE {qTable} SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = {qHistory}));
             """;

        // Writing to a history table needs versioning off, but not the PERIOD dropped: the
        // period columns are plain columns over there. So this is a lighter pair than
        // openSql/closeSql, and leaves the main table's GENERATED ALWAYS definition alone.
        var versioningOffSql = $"ALTER TABLE {qTable} SET (SYSTEM_VERSIONING = OFF);";
        var versioningOnSql = $"ALTER TABLE {qTable} SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = {qHistory}));";

        // The period columns and the key are deliberately absent: rewriting a period on a
        // history row breaks the timeline SetCurrentPeriodStart depends on, and silently, while
        // rewriting the key detaches the row from the entity it is history for.
        var historyColumns = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var property in root.GetProperties())
        {
            if (property.Name == periodStart ||
                property.Name == periodEnd ||
                property.Name == keyProperty.Name)
            {
                continue;
            }

            var column = property.GetColumnName(storeObject);
            if (column is null)
            {
                continue;
            }

            historyColumns[property.Name] = $"[{column}]";
        }

        return new(
            openSql,
            updateSql,
            closeSql,
            keyProperty.Name,
            versioningOffSql,
            versioningOnSql,
            qHistory,
            qKey,
            historyColumns);
    }

    public async Task Apply(DbContext db, object id, DateTime periodStart)
    {
        await Exec(db, OpenSql);
        try
        {
            await Exec(db, UpdateSql, periodStart, id);
        }
        finally
        {
            await Exec(db, CloseSql);
        }
    }

    /// <summary>
    /// Sets one column on every history row for <paramref name="id"/>. Reproduces states a
    /// deployed database can be in but a freshly migrated one never is - most usefully a NULL
    /// left behind when a column is dropped and re-added on a temporal pair, which SQL Server
    /// does not backfill into the rows already in the history table.
    /// <para>The column has to permit NULL in the database for a null <paramref name="value"/>.
    /// It cannot be widened here: SQL Server rejects re-enabling versioning when the current and
    /// history tables disagree on nullability, so such a row could not exist in production
    /// either. The case worth reproducing is the column that is nullable in the database while
    /// the model declares the property required.</para>
    /// </summary>
    public async Task SetHistoryColumn(DbContext db, object id, string propertyName, object? value)
    {
        if (!HistoryColumns.TryGetValue(propertyName, out var column))
        {
            throw new InvalidOperationException(
                $"'{propertyName}' is not a settable history column. The period columns are " +
                $"excluded, since changing those corrupts the temporal timeline. Available: " +
                string.Join(", ", HistoryColumns.Keys.Order()));
        }

        await Exec(db, VersioningOffSql);
        try
        {
            await Exec(db, $"UPDATE {HistoryTable} SET {column} = {{0}} WHERE {KeyColumn} = {{1}};", value, id);
        }
        finally
        {
            await Exec(db, VersioningOnSql);
        }
    }

    // Identifiers (table/column names) come from the EF model so cannot carry user input;
    // values are passed as positional parameters via FormattableString.
    static Task Exec(DbContext db, string sql, params object?[] args) =>
        db.Database.ExecuteSqlAsync(FormattableStringFactory.Create(sql, args));
}

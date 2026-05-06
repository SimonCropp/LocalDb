namespace EfLocalDb;

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

        return new(
            rawSchema ?? "dbo",
            table,
            root.GetHistoryTableSchema() ?? rawSchema ?? "dbo",
            historyTable,
            periodStart,
            periodEnd,
            keyColumn,
            keyProperty.Name);
    }

    public async Task Apply(DbContext db, object id, DateTime periodStart)
    {
        var qTable = $"[{Schema}].[{Table}]";
        var qHistory = $"[{HistorySchema}].[{HistoryTable}]";
        var qStart = $"[{PeriodStart}]";
        var qEnd = $"[{PeriodEnd}]";
        var qKey = $"[{KeyColumn}]";

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
}

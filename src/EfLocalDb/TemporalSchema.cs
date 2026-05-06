namespace EfLocalDb;

sealed record TemporalSchema(
    string OpenSql,
    string UpdateSql,
    string CloseSql,
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

        return new(openSql, updateSql, closeSql, keyProperty.Name);
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

    // Identifiers (table/column names) come from the EF model so cannot carry user input;
    // values are passed as positional parameters via FormattableString.
    static Task Exec(DbContext db, string sql, params object[] args) =>
        db.Database.ExecuteSqlAsync(FormattableStringFactory.Create(sql, args));
}

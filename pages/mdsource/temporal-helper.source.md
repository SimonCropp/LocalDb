# Temporal helper

`SetCurrentPeriodStart` re-stamps a row's `PeriodStart` (and aligns the most recent history row's `PeriodEnd`) on a [SQL Server temporal table](https://learn.microsoft.com/sql/relational-databases/tables/temporal-tables). It exists to give back-to-back `SaveChanges` calls in tests distinct, deterministic temporal timestamps without relying on `Task.Delay` between them.


## Why

When two `SaveChanges` calls land within the same `SYSUTCDATETIME()` tick, SQL Server collapses the resulting history row into a degenerate (zero-length) period and silently drops it. Tests that rely on querying a status transition via `TemporalAsOf` or by reading the history view then start failing nondeterministically. The traditional workaround is `await Task.Delay(...)` between saves, which is slow and brittle.

`SetCurrentPeriodStart` solves this by directly setting the current row's `PeriodStart` to a chosen value. All identifiers (table name, history table name and schema, period column names, primary key column) are read from the EF design-time model, so no raw SQL or hard-coded names appear at the call site.


## Configuring the entity

The helper relies on the entity being configured as temporal in `OnModelCreating`:

snippet: TemporalEntityConfig


## Usage

snippet: SetCurrentPeriodStartUsage

Two overloads are available:

- `db.SetCurrentPeriodStart<TEntity>(object id, DateTime periodStart)` — primitive form. Caller is responsible for any change-tracker updates afterward.
- `db.SetCurrentPeriodStart<TEntity>(TEntity entity, DateTime periodStart)` — convenience form. Extracts the primary key from `entity` via `db.Entry(entity)` and reloads it after the SQL UPDATE so the bumped `RowVersion` doesn't fail optimistic concurrency on the next `SaveChanges`.


## How it works

For each call the helper runs, in separate batches:

1. `ALTER TABLE ... SET (SYSTEM_VERSIONING = OFF)`
2. `ALTER TABLE ... DROP PERIOD FOR SYSTEM_TIME` — required because `GENERATED ALWAYS` is checked at batch parse time, not at execution time.
3. `UPDATE [Table] SET [PeriodStart] = @periodStart WHERE [Id] = @id`
4. `UPDATE [HistoryTable] SET [PeriodEnd] = @periodStart WHERE [Id] = @id AND [PeriodEnd] = (latest)` — keeps the timeline contiguous.
5. `ALTER TABLE ... ADD PERIOD FOR SYSTEM_TIME (...)`
6. `ALTER TABLE ... SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = ...))`

Steps 5 and 6 run in a `finally` so a failed UPDATE doesn't leave the table without versioning.


## Caveats

- `periodStart` must be **strictly greater** than the row's previous `PeriodStart`. Otherwise re-enabling system versioning fails with a period-overlap error.
- The anchor should be **after** any related entities the test depends on were created. `TemporalAsOf` joins through navigation properties as inner joins, so if a related row didn't exist at the anchor time, the temporal query returns no rows. Anchoring at `DateTime.UtcNow.AddSeconds(-10)` and stepping forward by milliseconds is usually safe.
- For TPH inheritance, the helper walks to the root entity type — calling it with a derived type works.
- Composite primary keys are not supported.
- The UPDATE bumps `RowVersion`. Use the entity overload, or call `db.Entry(entity).ReloadAsync()` yourself after the id overload.

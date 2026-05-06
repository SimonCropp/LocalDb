# Temporal helper

`SetCurrentPeriodStart` re-stamps a row's `PeriodStart` (and aligns the most recent history row's `PeriodEnd`) on a [SQL Server temporal table](https://learn.microsoft.com/sql/relational-databases/tables/temporal-tables). It exists to give back-to-back `SaveChanges` calls in tests distinct, deterministic temporal timestamps without relying on `Task.Delay` between them.

The method is exposed on both `SqlInstance<TDbContext>` and `SqlDatabase<TDbContext>`. Schema lookups (table, history table, period columns, PK column) are performed once at `SqlInstance` construction and cached, so per-call overhead is only the SQL execution.


## Why

When two `SaveChanges` calls land within the same `SYSUTCDATETIME()` tick, SQL Server collapses the resulting history row into a degenerate (zero-length) period and silently drops it. Tests that rely on querying a status transition via `TemporalAsOf` or by reading the history view then start failing nondeterministically. The traditional workaround is `await Task.Delay(...)` between saves, which is slow and brittle.

`SetCurrentPeriodStart` solves this by directly setting the current row's `PeriodStart` to a chosen value. All identifiers are read from the EF design-time model, so no raw SQL or hard-coded names appear at the call site.


## Configuring the entity

The helper relies on the entity being configured as temporal in `OnModelCreating`:

snippet: TemporalEntityConfig


## Usage

snippet: SetCurrentPeriodStartUsage

Two overloads are available on `SqlDatabase<TDbContext>`:

- `database.SetCurrentPeriodStart<TEntity>(object id, DateTime periodStart)` — primitive form. Caller is responsible for any change-tracker updates afterward.
- `database.SetCurrentPeriodStart<TEntity>(TEntity entity, DateTime periodStart)` — convenience form. Extracts the primary key from `entity` via `Context.Entry(entity)` and reloads it after the SQL UPDATE so the bumped `RowVersion` doesn't fail optimistic concurrency on the next `SaveChanges`.

`SqlInstance<TDbContext>` exposes the primitive form taking an explicit `TDbContext`, useful when operating outside a `SqlDatabase` scope.


## How it works

For each call the helper runs, in separate batches:

1. `ALTER TABLE ... SET (SYSTEM_VERSIONING = OFF)`
2. `ALTER TABLE ... DROP PERIOD FOR SYSTEM_TIME` — required because `GENERATED ALWAYS` is checked at batch parse time, not at execution time.
3. `UPDATE [Table] SET [PeriodStart] = @periodStart WHERE [Id] = @id`
4. `UPDATE [HistoryTable] SET [PeriodEnd] = @periodStart WHERE [Id] = @id AND [PeriodEnd] = (latest)` — keeps the timeline contiguous.
5. `ALTER TABLE ... ADD PERIOD FOR SYSTEM_TIME (...)`
6. `ALTER TABLE ... SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = ...))`

Steps 5 and 6 run in a `finally` so a failed UPDATE doesn't leave the table without versioning.


## Performance

Each call performs three round trips to SQL Server (opening DDL pair, the two UPDATEs combined, closing DDL pair). The dominant costs are:

- Four `ALTER TABLE` statements, each taking a `Sch-M` lock on the table and invalidating the plan cache for that table.
- The data-consistency check that runs when re-enabling system versioning. This validates that the current table's periods don't overlap any rows in the entire history table — it scales with history-row count, not with the entity being touched.

Typical cost on a small test database is around 5–15 ms per call. By comparison, replacing `await Task.Delay(TimeSpan.FromSeconds(5))` between four saves with four `SetCurrentPeriodStart` calls reduces test time from roughly 20 s to roughly 40 ms while making the timing deterministic.


### Scaling pitfalls

- **Avoid loops over many rows.** Calling `SetCurrentPeriodStart` after each of N inserts on the same table makes the Nth call re-validate all N existing history rows — overall cost is O(N²). For seeding many rows with backdated history, write a dedicated bulk path that disables versioning, inserts directly into the history table, then re-enables.
- **Plan cache flush.** Each call invalidates plans for the affected table. Subsequent EF queries pay one recompile each. Negligible for a few calls, measurable in the hundreds.


## Caveats

- `periodStart` must be **strictly greater** than the row's previous `PeriodStart`. Otherwise re-enabling system versioning fails with a period-overlap error.
- The anchor should be **after** any related entities the test depends on were created. `TemporalAsOf` joins through navigation properties as inner joins, so if a related row didn't exist at the anchor time, the temporal query returns no rows. Anchoring at `DateTime.UtcNow.AddSeconds(-10)` and stepping forward by milliseconds is usually safe.
- For TPH inheritance, the helper walks to the root entity type — calling it with a derived type works.
- Composite primary keys are not supported.
- The UPDATE bumps `RowVersion`. Use the entity overload, or call `Context.Entry(entity).ReloadAsync()` after the id overload.

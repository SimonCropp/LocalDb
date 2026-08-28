Mark test methods with `[PooledDb]` to lease a database from a fixed pool instead of creating one per test. The pool is built once from the template, and each test leases a database for its duration, writes inside a transaction, and rolls that transaction back on release so the next test sees the template state again.

Two costs disappear. The per-test file copy and attach is gone, and — usually the larger one — so is repeated query plan compilation: SQL Server keys the plan cache by database, so a database per test means every query is compiled afresh for every test and no plan is ever reused. A small pool lets those plans be reused for the rest of the run.

Pool size is `LocalDbSettings.PoolSize`, configurable via the `LocalDBPoolSize` environment variable and defaulting to `Environment.ProcessorCount`. It bounds how many pooled tests run concurrently, since a database is leased to one test at a time. Set it to `1` to serialise pooled tests onto a single database.

Not suited to every test:

 * Tests that need their changes committed, or that assert on state outside their own transaction.
 * Tests that assert on a timeline of changes. Inside one transaction every system-versioned temporal row shares the transaction start time, so a sequence of state changes collapses into a single instant.
 * On failure the database cannot be inspected, since the transaction is rolled back. When debugging, temporarily remove the attribute.

Those tests should be left to create a database per test.

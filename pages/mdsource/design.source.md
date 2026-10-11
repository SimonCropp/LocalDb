# Design

There is a tiered approach to the API.

For SQL:

SqlInstance > SqlDatabase > Connection

For EntityFramework:

SqlInstance > SqlDatabase > EfContext

SqlInstance represents a [SQL Sever instance](https://docs.microsoft.com/en-us/sql/database-engine/configure-windows/database-engine-instances-sql-server?#instances) (in this case hosted in SqlLocalDB) and SqlDatabase represents a [SQL Sever Database](https://docs.microsoft.com/en-us/sql/relational-databases/databases/databases?view=sql-server-2017) running inside that SqlInstance.

From a API perspective:

For SQL:

`SqlInstance` > `SqlDatabase` > `SqlConnection`

For EntityFramework:

`SqlInstance<TDbContext>` > `SqlDatabase<TDbContext>` > `TDbContext`/`SqlConnection`


Multiple SqlDatabases can exist inside each SqlInstance. Multiple DbContexts/SqlConnections can be created to talk to a SqlDatabase.

On the file system, each SqlInstance has corresponding directory and each SqlDatabase has a uniquely named mdf file within that directory.

When a SqlInstance is defined, a template database is created. All subsequent SqlDatabases created from that SqlInstance will be based on this template. The template allows schema and data to be created once, instead of every time a SqlDatabase is required. This results in improved performance by not requiring to re-create/re-migrate the SqlDatabase schema/data on each use.

The usual approach for consuming the API in a test project is as follows.

 * Single SqlInstance per test project.
 * Single SqlDatabase per test (or instance of a parameterized test).
 * One or more DbContexts/SqlConnections used within a test.

This assumes that there is a schema and data (and DbContext in the EntityFramework context) used for all tests. If those caveats are not correct then multiple SqlInstances can be used.


## Template database settings

When the template is built, a few database-level settings are applied. These persist in the template's `.mdf`/`.ldf` files and are inherited by every database attached from the template.

 * `auto_update_statistics off` — avoids background statistics updates causing nondeterministic test timing.
 * `delayed_durability forced` — a commit returns without waiting for its log records to be flushed to disk. A test database is disposable, so surviving a crash is worth nothing to it, and a test workload is mostly small commits that would each wait on that flush.
 * `page_verify none` — no checksum is computed when a page is written, or verified when it is read back. The checksum exists to detect storage corrupting a page, which is of no use on a database that is rebuilt from the template and deleted with the run.
 * `query_store off` — Query Store is on by default from SQL Server 2022. It keeps query and plan history, which nothing reads from a test database, and writes to each database in the background to do so.
 * `read_committed_snapshot on` — `READ COMMITTED` uses row versioning instead of shared locks. Required to prevent S/X-lock deadlocks between parallel transactional tests.

If the template files already exist on disk from a previous build, these settings are not reapplied — the template needs to be regenerated (delete the template files, or change the timestamp passed to `SqlInstance`) for new settings to take effect.


### Change detection that reads the transaction log

`delayed_durability forced` has one side effect a test can see. Queries are unaffected: a committed row is visible to every connection at once. But anything that detects changes by reading how far the transaction log has been flushed sees a commit only when its log records reach disk, which is some tens of milliseconds after the commit returned, at a moment of the server's choosing. [Delta](https://github.com/SimonCropp/Delta) is one: on SQL Server its `GetLastTimeStamp` reads `log_end_lsn` from `sys.dm_db_log_stats`.

A test that asserts on such a marker — that an ETag changed after a write, or did not change without one — should turn the setting back off for its database:

```sql
alter database current set delayed_durability = disabled;
exec sp_flush_log;
```

The flush is for commits made before the setting changed, the `alter` itself among them, which would otherwise still arrive late.


More information:

 * [Raw SqlConnection Usage](/pages/raw-usage.md)
 * [EntityFramework Usage](/pages/ef-usage.md)
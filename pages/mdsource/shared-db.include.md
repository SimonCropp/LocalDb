Mark test methods with `[SharedDb]` to share a single database across all query-only tests. Instead of cloning the template for each test, a shared database is created once and reused. This eliminates per-test DB creation overhead for tests that only read data.

Use `[SharedDbWithTransaction]` instead when tests need to write data. Each test runs inside an auto-rolling-back transaction, ensuring test isolation while still sharing the database instance.

Note: `[SharedDbWithTransaction]` means that on test failure the resulting database cannot be inspected (since the transaction is rolled back). A workaround when debugging a failure is to temporarily remove the attribute.

Both attributes can be mixed in the same test fixture:
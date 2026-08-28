Mark test methods with `[SharedDb]` to share a single database across all query-only tests. Instead of cloning the template for each test, a shared database is created once and reused. This eliminates per-test DB creation overhead for tests that only read data.

The shared database is read-only: attempting to save changes throws. Tests that need to write should use `[PooledDb]` instead.

Mark test methods with `[SharedDb]` to share a single database across all query-only tests. Instead of cloning the template for each test, a shared database is created once and reused. This eliminates per-test DB creation overhead for tests that only read data.

`[SharedDb]` can also be applied to a test class, or to the assembly with `[assembly: SharedDb]`. The nearest attribute wins: a method attribute overrides a class attribute, which overrides an assembly attribute. Mark a test method or class with `[NewDb]` to opt it out and create a database per test. Applying more than one of `[PooledDb]`, `[SharedDb]` and `[NewDb]` to the same method, class, or assembly throws.

The shared database is read-only and any write throws, not only `SaveChanges`: `ExecuteUpdate`, `ExecuteDelete`, `ExecuteSqlRaw` and hand-written commands are blocked too. Tests that need to write should use `[PooledDb]` instead.

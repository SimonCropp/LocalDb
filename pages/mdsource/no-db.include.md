Mark a test method with `[NoDb]` when it does not touch the database, for example a test that renders a document, formats output, or runs a validator. The test still inherits from `LocalDbTestBase`, so it can live next to related database tests, but no database is built for it.

A test without any attribute gets a new database per test, which is the most expensive mode. So a database-free test in a `LocalDbTestBase` class should be marked `[NoDb]` rather than left unmarked.

In a `[NoDb]` test `Database` is null, and accessing `ArrangeData`, `ActData`, or `AssertData`, or calling `Reset`, throws.

`[NoDb]` can also be applied to a test class. The nearest attribute wins, so a method marked `[PooledDb]`, `[SharedDb]`, or `[NewDb]` in a `[NoDb]` class gets a database. Applying `[NoDb]` together with `[PooledDb]`, `[SharedDb]` or `[NewDb]` to the same method or class throws.

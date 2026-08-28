namespace EfLocalDb;

/// <summary>
/// Runs the test against a database leased from a fixed pool, rather than creating a database
/// for the test. Writes are wrapped in a transaction that is rolled back when the test ends.
/// <para>
/// Pool size is <see cref="LocalDbSettings.PoolSize" /> and bounds how many pooled tests run
/// concurrently. Not suited to tests that need their changes committed, or that assert on state
/// outside their own transaction.
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class PooledDbAttribute : Attribute;

namespace EfLocalDb;

/// <summary>
/// Runs the test against a database leased from a fixed pool, rather than creating a database
/// for the test. Writes are wrapped in a transaction that is rolled back when the test ends.
/// <para>
/// Pool size is <see cref="LocalDbSettings.PoolSize" /> and bounds how many pooled tests run
/// concurrently. Not suited to tests that need their changes committed, or that assert on state
/// outside their own transaction.
/// </para>
/// <para>
/// Can be applied to a test method, a test class, or the assembly. The nearest wins, so a method
/// marked <see cref="SharedDbAttribute" /> in a <see cref="PooledDbAttribute" /> class uses a shared
/// database. Use <see cref="NewDbAttribute" /> to opt a method or class out.
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly)]
public sealed class PooledDbAttribute : Attribute;

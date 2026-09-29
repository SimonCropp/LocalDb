namespace EfLocalDb;

/// <summary>
/// Runs the test without a database. Use for tests in a LocalDbTestBase class that do not touch
/// the database, such as rendering, formatting, or validation tests, so they skip the cost of
/// building one. In such a test <c>Database</c> is null, and accessing <c>ArrangeData</c>,
/// <c>ActData</c>, or <c>AssertData</c>, or calling <c>Reset</c>, throws.
/// <para>
/// Can be applied to a test method or a test class. The nearest wins, so a method marked
/// <see cref="PooledDbAttribute" /> in a <see cref="NoDbAttribute" /> class uses a pooled database.
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class NoDbAttribute : Attribute;

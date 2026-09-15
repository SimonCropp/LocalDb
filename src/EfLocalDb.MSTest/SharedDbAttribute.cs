namespace EfLocalDb;

/// <summary>
/// Runs the test against a single read-only database shared by all <see cref="SharedDbAttribute" /> tests.
/// <para>
/// Can be applied to a test method, a test class, or the assembly. The nearest wins, so a method
/// marked <see cref="PooledDbAttribute" /> in a <see cref="SharedDbAttribute" /> class uses a pooled
/// database. Use <see cref="NewDbAttribute" /> to opt a method or class out.
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly)]
public sealed class SharedDbAttribute : Attribute;

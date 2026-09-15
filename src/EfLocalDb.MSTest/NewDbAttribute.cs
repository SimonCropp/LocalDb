namespace EfLocalDb;

/// <summary>
/// Runs the test against a new database built from the template. This is the default when no
/// attribute applies, so it is only needed to opt a method or class out of a
/// <see cref="SharedDbAttribute" /> or <see cref="PooledDbAttribute" /> applied to its class or assembly.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class NewDbAttribute : Attribute;

namespace EfLocalDb;

/// <summary>
/// Removed. Use <see cref="PooledDbAttribute" />.
/// </summary>
/// <remarks>
/// Retained purely so existing usages fail with the message below rather than an
/// unresolved-name error. It is no longer honoured by any test base.
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
[Obsolete("Removed. Use [PooledDb], which leases from a pool of databases rather than sharing one. Set LocalDbSettings.PoolSize = 1 for the previous single-database behaviour. Unlike [SharedDbWithTransaction], a pooled database is leased to one test at a time, so concurrent tests cannot contend for locks on the same database.", error: true)]
public sealed class SharedDbWithTransactionAttribute : Attribute;

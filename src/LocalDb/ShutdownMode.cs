#if EF
namespace EfLocalDb;
#else
namespace LocalDb;
#endif

/// <summary>
/// Provides options for how to shut down the instance.
/// </summary>
/// <remarks>
/// See https://docs.microsoft.com/sql/relational-databases/express-localdb-instance-apis/localdbstopinstance-function
/// </remarks>
public enum ShutdownMode
{
    /// <summary>
    /// Shutdown cleanly, using the <c>SHUTDOWN</c> T-SQL command.
    /// See https://docs.microsoft.com/sql/t-sql/language-elements/shutdown-transact-sql
    /// </summary>
    UseSqlShutdown = 0,

    /// <summary>
    /// Shutdown immediately, by asking the operating system to kill the process hosting the instance.
    /// The database may be corrupted by this approach, so only use when you intend to delete the instance.
    /// </summary>
    KillProcess = 1,

    /// <summary>
    /// Shutdown using the <c>SHUTDOWN WITH NOWAIT</c> T-SQL command, which does not perform checkpoints in the database.
    /// See https://docs.microsoft.com/sql/t-sql/language-elements/shutdown-transact-sql
    /// </summary>
    UseSqlShutdownWithNoWait = 2
}

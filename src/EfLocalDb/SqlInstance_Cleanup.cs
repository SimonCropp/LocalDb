namespace EfLocalDb;

public partial class SqlInstance<TDbContext>
    where TDbContext : DbContext
{
    /// <summary>
    /// Deletes the LocalDB instance and all associated database files.
    /// Use this to clean up after tests are complete.
    /// </summary>
    public void Cleanup()
    {
        Guard.AgainstBadOS();
        Wrapper.DeleteInstance();
    }

    /// <summary>
    /// Deletes the LocalDB instance and all associated database files with the specified shutdown mode.
    /// </summary>
    /// <param name="mode">The shutdown mode to use when stopping the LocalDB instance.</param>
    public void Cleanup(ShutdownMode mode)
    {
        Guard.AgainstBadOS();
        Wrapper.DeleteInstance(mode);
    }

    /// <summary>
    /// Deletes the LocalDB instance and all associated database files with the specified shutdown mode and timeout.
    /// </summary>
    /// <param name="mode">The shutdown mode to use when stopping the LocalDB instance.</param>
    /// <param name="timeout">The maximum time to wait for the instance to stop.</param>
    public void Cleanup(ShutdownMode mode, TimeSpan timeout)
    {
        Guard.AgainstBadOS();
        Wrapper.DeleteInstance(mode, timeout);
    }
}
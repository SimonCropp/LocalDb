// ReSharper disable RedundantCast

namespace EfLocalDb;

public partial class SqlInstance<TDbContext>
    where TDbContext : DbContext
{
    public void Cleanup()
    {
        Guard.AgainstBadOS();
        Wrapper.DeleteInstance();
    }

    public void Cleanup(ShutdownMode mode)
    {
        Guard.AgainstBadOS();
        Wrapper.DeleteInstance(mode);
    }

    public void Cleanup(ShutdownMode mode, TimeSpan timeout)
    {
        Guard.AgainstBadOS();
        Wrapper.DeleteInstance(mode, timeout);
    }
}
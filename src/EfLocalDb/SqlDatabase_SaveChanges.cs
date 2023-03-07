namespace EfLocalDb;

public partial class SqlDatabase<TDbContext>
{
    /// <summary>
    ///     Calls <see cref="DbContext.SaveChanges()" /> on <see cref="Context" />.
    /// </summary>
    public Task<int> SaveChangesAsync()
    {
        ThrowForNoChanges();
        return Context.SaveChangesAsync();
    }

    void ThrowForNoChanges()
    {
        if (!Context.ChangeTracker.HasChanges())
        {
            throw new("No pending changes. It is possible Find or Single has been used, and the returned entity then modified. Find or Single use a non tracking context. Use the Context to dor modifications.");
        }
    }

    /// <summary>
    ///     Calls <see cref="DbContext.SaveChanges(bool)" /> on <see cref="Context" />.
    /// </summary>
    public int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ThrowForNoChanges();
        return Context.SaveChanges(acceptAllChangesOnSuccess);
    }

    /// <summary>
    ///     Calls <see cref="DbContext.SaveChangesAsync(CancellationToken)" /> on <see cref="Context" />.
    /// </summary>
    public Task<int> SaveChangesAsync(Cancellation cancellation = default)
    {
        ThrowForNoChanges();
        return Context.SaveChangesAsync(cancellation);
    }

    /// <summary>
    ///     Calls <see cref="DbContext.SaveChangesAsync(bool, CancellationToken)" /> on <see cref="Context" />.
    /// </summary>
    public Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, Cancellation cancellation = default)
    {
        ThrowForNoChanges();
        return Context.SaveChangesAsync(acceptAllChangesOnSuccess, cancellation);
    }
}
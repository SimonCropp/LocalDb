namespace EfLocalDb;

public partial class SqlDatabase<TDbContext>
{
    /// <summary>
    /// Saves all changes made in <see cref="Context"/> to the database.
    /// Throws if there are no pending changes to prevent accidental no-op saves.
    /// </summary>
    /// <returns>A task containing the number of state entries written to the database.</returns>
    /// <exception cref="Exception">Thrown if there are no pending changes in the change tracker.</exception>
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
    /// Saves all changes made in <see cref="Context"/> to the database.
    /// Throws if there are no pending changes to prevent accidental no-op saves.
    /// </summary>
    /// <param name="acceptAllChangesOnSuccess">
    /// Indicates whether <see cref="Microsoft.EntityFrameworkCore.ChangeTracking.ChangeTracker.AcceptAllChanges"/>
    /// is called after the changes have been sent successfully to the database.
    /// </param>
    /// <returns>The number of state entries written to the database.</returns>
    /// <exception cref="Exception">Thrown if there are no pending changes in the change tracker.</exception>
    public int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ThrowForNoChanges();
        return Context.SaveChanges(acceptAllChangesOnSuccess);
    }

    /// <summary>
    /// Saves all changes made in <see cref="Context"/> to the database.
    /// Throws if there are no pending changes to prevent accidental no-op saves.
    /// </summary>
    /// <param name="cancel">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A task containing the number of state entries written to the database.</returns>
    /// <exception cref="Exception">Thrown if there are no pending changes in the change tracker.</exception>
    public Task<int> SaveChangesAsync(Cancel cancel = default)
    {
        ThrowForNoChanges();
        return Context.SaveChangesAsync(cancel);
    }

    /// <summary>
    /// Saves all changes made in <see cref="Context"/> to the database.
    /// Throws if there are no pending changes to prevent accidental no-op saves.
    /// </summary>
    /// <param name="acceptAllChangesOnSuccess">
    /// Indicates whether <see cref="Microsoft.EntityFrameworkCore.ChangeTracking.ChangeTracker.AcceptAllChanges"/>
    /// is called after the changes have been sent successfully to the database.
    /// </param>
    /// <param name="cancel">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A task containing the number of state entries written to the database.</returns>
    /// <exception cref="Exception">Thrown if there are no pending changes in the change tracker.</exception>
    public Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, Cancel cancel = default)
    {
        ThrowForNoChanges();
        return Context.SaveChangesAsync(acceptAllChangesOnSuccess, cancel);
    }
}
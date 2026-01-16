namespace EfLocalDb;

public partial class SqlDatabase<TDbContext>
{
    /// <summary>
    /// Removes entities from the database using <see cref="Context"/> and saves changes.
    /// Entities are removed from the appropriate DbSet based on their runtime type.
    /// </summary>
    /// <param name="entities">The entities to remove.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task RemoveData(IEnumerable<object> entities) =>
        Context.RemoveData(entities, instance.EntityTypes);

    /// <summary>
    /// Removes entities from the database using <see cref="Context"/> and saves changes.
    /// Entities are removed from the appropriate DbSet based on their runtime type.
    /// </summary>
    /// <param name="entities">The entities to remove.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task RemoveData(params object[] entities) =>
        RemoveData((IEnumerable<object>) entities);

    /// <summary>
    /// Removes entities from the database using a new untracked DbContext and saves changes.
    /// Use this when you want to remove data without affecting the <see cref="Context"/> change tracker.
    /// </summary>
    /// <param name="entities">The entities to remove.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task RemoveDataUntracked(IEnumerable<object> entities)
    {
        await using var context = NewDbContext();
        await context.RemoveData(entities, instance.EntityTypes);
    }

    /// <summary>
    /// Removes entities from the database using a new untracked DbContext and saves changes.
    /// Use this when you want to remove data without affecting the <see cref="Context"/> change tracker.
    /// </summary>
    /// <param name="entities">The entities to remove.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task RemoveDataUntracked(params object[] entities) => RemoveDataUntracked((IEnumerable<object>) entities);
}
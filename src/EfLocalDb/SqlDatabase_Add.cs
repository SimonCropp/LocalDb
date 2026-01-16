namespace EfLocalDb;

public partial class SqlDatabase<TDbContext>
{
    /// <summary>
    /// Adds entities to the database using <see cref="Context"/> and saves changes.
    /// Entities are added to the appropriate DbSet based on their runtime type.
    /// </summary>
    /// <param name="entities">The entities to add.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task AddData(IEnumerable<object> entities) =>
        Context.AddData(entities, instance.EntityTypes);

    /// <summary>
    /// Adds entities to the database using <see cref="Context"/> and saves changes.
    /// Entities are added to the appropriate DbSet based on their runtime type.
    /// </summary>
    /// <param name="entities">The entities to add.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task AddData(params object[] entities) =>
        AddData((IEnumerable<object>) entities);

    /// <summary>
    /// Adds entities to the database using a new untracked DbContext and saves changes.
    /// Use this when you want to add data without affecting the <see cref="Context"/> change tracker.
    /// </summary>
    /// <param name="entities">The entities to add.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task AddDataUntracked(IEnumerable<object> entities)
    {
        await using var context = NewDbContext();
        await context.AddData(entities, instance.EntityTypes);
    }

    /// <summary>
    /// Adds entities to the database using a new untracked DbContext and saves changes.
    /// Use this when you want to add data without affecting the <see cref="Context"/> change tracker.
    /// </summary>
    /// <param name="entities">The entities to add.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task AddDataUntracked(params object[] entities) => AddDataUntracked((IEnumerable<object>) entities);
}
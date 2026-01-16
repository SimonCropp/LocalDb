namespace EfLocalDb;

public partial class SqlDatabase<TDbContext>
{
    /// <summary>
    /// Counts the number of entities of type <typeparamref name="T"/> in the database,
    /// optionally filtered by a predicate. Uses <see cref="NoTrackingContext"/> for efficient read-only queries.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="predicate">An optional expression to filter entities. If null, counts all entities.</param>
    /// <returns>The number of matching entities.</returns>
    public Task<int> Count<T>(Expression<Func<T, bool>>? predicate = null)
        where T : class
    {
        if (predicate is null)
        {
            return Set<T>().CountAsync();
        }

        return Set<T>().CountAsync(predicate);
    }
}
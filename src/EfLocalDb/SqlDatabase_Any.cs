namespace EfLocalDb;

public partial class SqlDatabase<TDbContext>
{
    /// <summary>
    /// Determines whether any entities of type <typeparamref name="T"/> exist in the database,
    /// optionally filtered by a predicate. Uses <see cref="NoTrackingContext"/> for efficient read-only queries.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="predicate">An optional expression to filter entities. If null, checks if any entities exist.</param>
    /// <returns>True if any matching entities exist; otherwise, false.</returns>
    public Task<bool> Any<T>(Expression<Func<T, bool>>? predicate = null)
        where T : class =>
        AnyAsync(Set<T>(), predicate);

    /// <summary>
    /// Determines whether any entities of type <typeparamref name="T"/> exist in the database,
    /// ignoring any global query filters. Uses <see cref="NoTrackingContext"/> for efficient read-only queries.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="predicate">An optional expression to filter entities. If null, checks if any entities exist.</param>
    /// <returns>True if any matching entities exist; otherwise, false.</returns>
    public Task<bool> AnyIgnoreFilters<T>(Expression<Func<T, bool>>? predicate = null)
        where T : class =>
        AnyAsync(Set<T>().IgnoreQueryFilters(), predicate);

    static Task<bool> AnyAsync<T>(IQueryable<T> set, Expression<Func<T, bool>>? predicate) where T : class
    {
        if (predicate is null)
        {
            return set.AnyAsync();
        }

        return set.AnyAsync(predicate);
    }
}
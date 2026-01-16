namespace EfLocalDb;

public partial class SqlDatabase<TDbContext>
{
    /// <summary>
    /// Returns the single entity of type <typeparamref name="T"/> from the database,
    /// optionally filtered by a predicate. Uses <see cref="NoTrackingContext"/> for efficient read-only queries.
    /// Throws if zero or more than one entity matches.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="predicate">An optional expression to filter entities. If null, expects exactly one entity in the set.</param>
    /// <returns>The single matching entity.</returns>
    /// <exception cref="InvalidOperationException">Thrown if zero or more than one entity matches.</exception>
    public Task<T> Single<T>(Expression<Func<T, bool>>? predicate = null)
        where T : class =>
        Single(Set<T>(), predicate);

    /// <summary>
    /// Returns the single entity of type <typeparamref name="T"/> from the database,
    /// ignoring any global query filters. Uses <see cref="NoTrackingContext"/> for efficient read-only queries.
    /// Throws if zero or more than one entity matches.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="predicate">An optional expression to filter entities. If null, expects exactly one entity in the set.</param>
    /// <returns>The single matching entity.</returns>
    /// <exception cref="InvalidOperationException">Thrown if zero or more than one entity matches.</exception>
    public Task<T> SingleIgnoreFilters<T>(Expression<Func<T, bool>>? predicate = null)
        where T : class =>
        Single(Set<T>().IgnoreQueryFilters(), predicate);

    static Task<T> Single<T>(IQueryable<T> set, Expression<Func<T, bool>>? predicate)
        where T : class
    {
        if (predicate is null)
        {
            return set.SingleAsync();
        }

        return set.SingleAsync(predicate);
    }
}
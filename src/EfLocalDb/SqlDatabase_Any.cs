namespace EfLocalDb;

public partial class SqlDatabase<TDbContext>
{
    /// <summary>
    ///     Calls <see cref="DbSet{TEntity}.FindAsync(object[])" /> on the <see cref="DbContext.Set{TEntity}()" /> for
    ///     <typeparamref name="T" />.
    /// </summary>
    public Task<bool> Any<T>(Expression<Func<T, bool>>? predicate = null)
        where T : class =>
        AnyAsync(Set<T>(), predicate);

    /// <summary>
    ///     Calls <see cref="DbSet{TEntity}.FindAsync(object[])" /> on the <see cref="DbContext.Set{TEntity}()" /> for
    ///     <typeparamref name="T" />.
    /// </summary>
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
namespace EfLocalDb;

public partial class SqlDatabase<TDbContext>
{
    /// <summary>
    ///     Calls <see cref="DbSet{TEntity}.FindAsync(object[])" /> on the <see cref="DbContext.Set{TEntity}()" /> for
    ///     <typeparamref name="T" />.
    /// </summary>
    public Task<bool> Any<T>(Expression<Func<T, bool>>? predicate = null)
        where T : class
    {
        var set = Set<T>();

        if (predicate is null)
        {
            return set.AnyAsync();
        }

        return set.AnyAsync(predicate);
    }
}
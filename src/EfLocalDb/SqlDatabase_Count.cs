namespace EfLocalDb;

public partial class SqlDatabase<TDbContext>
{
    /// <summary>
    ///     Calls <see cref="EntityFrameworkQueryableExtensions.CountAsync{TSource}(IQueryable{TSource}, CancellationToken)" />
    ///     on the <see cref="DbContext.Set{TEntity}()" /> for <typeparamref name="T" />.
    /// </summary>
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
namespace EfLocalDb;

public partial class SqlDatabase<TDbContext>
{
    /// <summary>
    ///     Calls <see cref="DbSet{TEntity}.FindAsync(object[])" /> on the <see cref="DbContext.Set{TEntity}()" /> for
    ///     <typeparamref name="T" />.
    /// </summary>
    [Obsolete]
    public Task<T> Find<T>(params object[] keys)
        where T : class =>
        InnerFind<T>(NoTrackingContext, keys, false);

    /// <summary>
    ///     Calls <see cref="DbSet{TEntity}.FindAsync(object[])" /> on the <see cref="DbContext.Set{TEntity}()" /> for
    ///     <typeparamref name="T" />.
    /// </summary>
    [Obsolete]
    public Task<T> FindIgnoreFilters<T>(params object[] keys)
        where T : class =>
        InnerFind<T>(NoTrackingContext, keys, true);

    internal async Task<T> InnerFind<T>(TDbContext context, object[] keys, bool ignoreFilters) where T : class
    {
        var key = instance.FindKey<T>(keys, out var find);

        var result = await SqlInstance<TDbContext>.InvokeFind(context, ignoreFilters, keys, find, key);
        if (result is not null)
        {
            return (T) result;
        }

        var keyString = string.Join(", ", keys);
        throw new($"No record found with keys: {keyString}");
    }

    /// <summary>
    ///     Calls <see cref="DbContext.FindAsync(Type,object[])" /> on all entity types and returns all resulting items.
    /// </summary>
    [Obsolete]
    public Task<object> Find(params object[] keys) =>
        InnerFind(NoTrackingContext, false, keys);

    /// <summary>
    ///     Calls <see cref="DbContext.FindAsync(Type,object[])" /> on all entity types and returns all resulting items.
    /// </summary>
    [Obsolete]
    public Task<object> FindIgnoreFilters(params object[] keys) =>
        InnerFind(NoTrackingContext, true, keys);

    internal async Task<object> InnerFind(TDbContext context, bool ignoreFilters, object[] keys)
    {
        var results = await instance.FindResults(context, ignoreFilters, keys);

        if (results.Count == 1)
        {
            return results[0];
        }

        if (results.Count > 1)
        {
            throw new MoreThanOneException(keys, results);
        }

        throw new($"No record found with keys: {string.Join(", ", keys)}");
    }
}
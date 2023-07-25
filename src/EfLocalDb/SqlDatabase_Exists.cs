namespace EfLocalDb;

public partial class SqlDatabase<TDbContext>
{
    /// <summary>
    ///     Calls <see cref="DbContext.FindAsync(Type,object[])" /> on all entity types and returns true if the item exists.
    /// </summary>
    public Task<bool> Exists<T>(params object[] keys)
        where T : class =>
        Exists(Set<T>(), keys);

    /// <summary>
    ///     Calls <see cref="DbContext.FindAsync(Type,object[])" /> on all entity types and returns true if the item exists.
    /// </summary>
    public Task<bool> ExistsIgnoreFilters<T>(params object[] keys)
        where T : class =>
        Exists(Set<T>().IgnoreQueryFilters(), keys);

    Task<bool> Exists<T>(IQueryable<T> set, object[] keys) where T : class
    {
        var entityType = instance.EntityTypes.Single(_ => _.ClrType == typeof(T));
        var primaryKey = entityType.FindPrimaryKey();
        if (primaryKey == null)
        {
            throw new($"{typeof(T).FullName} does not have a primary key");
        }

        var lambda = Lambda<T>.Build(primaryKey.Properties, new(keys));
        return set.AnyAsync(lambda);
    }

    /// <summary>
    ///     Calls <see cref="DbContext.FindAsync(Type,object[])" /> on all entity types and returns true if the item exists.
    /// </summary>
    public Task<bool> Exists(params object[] keys) =>
        InnerExists(NoTrackingContext, false, keys);

    /// <summary>
    ///     Calls <see cref="DbContext.FindAsync(Type,object[])" /> on all entity types and returns true if the item exists.
    /// </summary>
    public Task<bool> ExistsIgnoreFilter(params object[] keys) =>
        InnerExists(NoTrackingContext, true, keys);

    internal async Task<bool> InnerExists(TDbContext context, bool ignoreFilters, object[] keys)
    {
        var results = await instance.FindResults(context, ignoreFilters, keys);

        if (results.Count == 1)
        {
            return true;
        }

        if (results.Count > 1)
        {
            throw new MoreThanOneException(keys, results);
        }

        return false;
    }
}
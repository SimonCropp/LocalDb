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
        var entityType = EntityTypes.Single(_ => _.ClrType == typeof(T));
        var primaryKey = entityType.FindPrimaryKey();
        if (primaryKey == null)
        {
            throw new($"{typeof(T).FullName} does not have a primary key");
        }

        var lambda = BuildLambda<T>(primaryKey.Properties, new(keys));
        return set.AnyAsync(lambda);
    }

    /// <summary>
    ///     Calls <see cref="DbContext.FindAsync(Type,object[])" /> on all entity types and returns true if the item exists.
    /// </summary>
    public Task<bool> Exists(params object[] keys) =>
        InnerExists(false, keys);

    /// <summary>
    ///     Calls <see cref="DbContext.FindAsync(Type,object[])" /> on all entity types and returns true if the item exists.
    /// </summary>
    public Task<bool> ExistsIgnoreFilter(params object[] keys) =>
        InnerExists(true, keys);

    async Task<bool> InnerExists(bool ignoreFilters, object[] keys)
    {
        var results = await FindResults(ignoreFilters, keys);

        if (results.Count == 1)
        {
            return true;
        }

        if (results.Count <= 1)
        {
            return false;
        }

        var keyString = string.Join(", ", keys);
        throw new($"More than one record found with keys: {keyString}");
    }

    static Expression<Func<T, bool>> BuildLambda<T>(IReadOnlyList<IProperty> keyProperties, ValueBuffer keyValues)
    {
        var parameter = Expression.Parameter(typeof(T), "e");

        var predicate = Microsoft.EntityFrameworkCore.Internal.ExpressionExtensions.BuildPredicate(keyProperties, keyValues, parameter);
        return Expression.Lambda<Func<T, bool>>(predicate, parameter);
    }
}
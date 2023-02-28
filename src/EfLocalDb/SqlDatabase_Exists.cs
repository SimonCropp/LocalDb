namespace EfLocalDb;

public partial class SqlDatabase<TDbContext>
{
    /// <summary>
    ///     Calls <see cref="DbContext.FindAsync(Type,object[])" /> on all entity types and returns true if the item exists.
    /// </summary>
    public Task<bool> Exists<T>(params object[] keys)
        where T : class
    {
        var set = Set<T>();
        var primaryKey = set.EntityType.FindPrimaryKey();
        if (primaryKey == null)
        {
            throw new($"{typeof(T).FullName} does not have a primary key");
        }

        return set.AnyAsync(BuildLambda<T>(primaryKey.Properties, new(keys)));
    }

    /// <summary>
    ///     Calls <see cref="DbContext.FindAsync(Type,object[])" /> on all entity types and returns true if the item exists.
    /// </summary>
    public async Task<bool> Exists(params object[] keys)
    {
        var results = await FindResults(keys);

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
        var entityParameter = Expression.Parameter(typeof(T), "e");

        var predicate = Microsoft.EntityFrameworkCore.Internal.ExpressionExtensions.BuildPredicate(keyProperties, keyValues, entityParameter);
        return Expression.Lambda<Func<T, bool>>(predicate, entityParameter);
    }
}
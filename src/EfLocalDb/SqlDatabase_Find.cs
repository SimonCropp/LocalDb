namespace EfLocalDb;

public partial class SqlDatabase<TDbContext>
{
    /// <summary>
    ///     Calls <see cref="DbSet{TEntity}.FindAsync(object[])" /> on the <see cref="DbContext.Set{TEntity}()" /> for
    ///     <typeparamref name="T" />.
    /// </summary>
    public async Task<T> Find<T>(params object[] keys)
        where T : class
    {
        var result = await Set<T>().FindAsync(keys);
        if (result is not null)
        {
            return result;
        }

        var keyString = string.Join(", ", keys);
        throw new($"No record found with keys: {keyString}");
    }

    /// <summary>
    ///     Calls <see cref="DbContext.FindAsync(Type,object[])" /> on all entity types and returns all resulting items.
    /// </summary>
    public Task<object> Find(params object[] keys) =>
        InnerFind(false, keys);

    /// <summary>
    ///     Calls <see cref="DbContext.FindAsync(Type,object[])" /> on all entity types and returns all resulting items.
    /// </summary>
    public Task<object> FindIgnoreFilters(params object[] keys) =>
        InnerFind(true, keys);

    async Task<object> InnerFind(bool ignoreFilters, object[] keys)
    {
        var results = await FindResults(ignoreFilters, keys);

        if (results.Count == 1)
        {
            return results[0];
        }

        var keyString = string.Join(", ", keys);

        if (results.Count > 1)
        {
            throw new($"More than one record found with keys: {keyString}");
        }

        throw new($"No record found with keys: {keyString}");
    }

    async Task<List<object>> FindResults(bool ignoreFilters, object[] keys)
    {
        var list = new List<object>();

        var inputKeyTypes = keys.Select(_ => _.GetType()).ToList();

        foreach (var (keyTypes, key, find) in entityKeyMap)
        {
            if (!keyTypes.SequenceEqual(inputKeyTypes))
            {
                continue;
            }

            var result = await (Task<object?>) find.Invoke(
                this,
                new object?[]
                {
                    ignoreFilters,
                    key,
                    keys
                })!;
            if (result is not null)
            {
                list.Add(result);
            }
        }

        return list;
    }

    async Task<object?> FindResult<T>(bool ignoreFilters, IKey key, object[] keys)
        where T : class
    {
        var lambda = BuildLambda<T>(key.Properties, new(keys));
        IQueryable<T> set = NoTrackingContext.Set<T>();
        if (ignoreFilters)
        {
            set = set.IgnoreQueryFilters();
        }

        return await set.FirstOrDefaultAsync(lambda);
    }
}
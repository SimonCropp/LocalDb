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
    public async Task<object> Find(params object[] keys)
    {
        var results = await FindResults(keys);

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

    async Task<List<object>> FindResults(object[] keys)
    {
        var list = new List<object>();

        var inputKeyTypes = keys.Select(_ => _.GetType()).ToList();

        foreach (var (entity, key) in entityKeyMap)
        {
            if (!key.Properties.Select(_ => _.ClrType).SequenceEqual(inputKeyTypes))
            {
                continue;
            }

            var genericFindResult = findResult.MakeGenericMethod(entity.ClrType);
            var result = await (Task<object?>) genericFindResult.Invoke(
                this,
                new object?[]
                {
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

    async Task<object?> FindResult<T>(IKey key,object[] keys)
        where T : class
    {
        var lambda = BuildLambda<T>(key.Properties, new(keys));
        return await NoTrackingContext.Set<T>().FirstOrDefaultAsync(lambda);
    }
}
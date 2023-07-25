﻿namespace EfLocalDb;

public partial class SqlDatabase<TDbContext>
{
    /// <summary>
    ///     Calls <see cref="DbSet{TEntity}.FindAsync(object[])" /> on the <see cref="DbContext.Set{TEntity}()" /> for
    ///     <typeparamref name="T" />.
    /// </summary>
    public Task<T> Find<T>(params object[] keys)
        where T : class =>
        InnerFind<T>(NoTrackingContext, keys, false);

    /// <summary>
    ///     Calls <see cref="DbSet{TEntity}.FindAsync(object[])" /> on the <see cref="DbContext.Set{TEntity}()" /> for
    ///     <typeparamref name="T" />.
    /// </summary>
    public Task<T> FindIgnoreFilters<T>(params object[] keys)
        where T : class =>
        InnerFind<T>(NoTrackingContext, keys, true);

    internal async Task<T> InnerFind<T>(TDbContext context, object[] keys, bool ignoreFilters) where T : class
    {
        var (_, keyTypes, key, find) = entityKeyMap.Single(_ => _.Entity.ClrType == typeof(T));

        var inputKeyTypes = keys.Select(_ => _.GetType()).ToList();
        if (!keyTypes.SequenceEqual(inputKeyTypes))
        {
            throw new("Key types dont match");
        }

        var result = await InvokeFind(context, ignoreFilters, keys, find, key);
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
    public Task<object> Find(params object[] keys) =>
        InnerFind(NoTrackingContext, false, keys);

    /// <summary>
    ///     Calls <see cref="DbContext.FindAsync(Type,object[])" /> on all entity types and returns all resulting items.
    /// </summary>
    public Task<object> FindIgnoreFilters(params object[] keys) =>
        InnerFind(NoTrackingContext, true, keys);

    internal async Task<object> InnerFind(TDbContext context, bool ignoreFilters, object[] keys)
    {
        var results = await FindResults(context, ignoreFilters, keys);

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

    async Task<List<object>> FindResults(TDbContext context, bool ignoreFilters, object[] keys)
    {
        var list = new List<object>();

        var inputKeyTypes = keys.Select(_ => _.GetType()).ToList();

        foreach (var (_, keyTypes, key, find) in entityKeyMap)
        {
            if (!keyTypes.SequenceEqual(inputKeyTypes))
            {
                continue;
            }

            var result = await InvokeFind(context, ignoreFilters, keys, find, key);
            if (result is not null)
            {
                list.Add(result);
            }
        }

        return list;
    }

    Task<object?> InvokeFind(TDbContext context, bool ignoreFilters, object[] keys, MethodInfo find, IKey key) =>
        (Task<object?>) find.Invoke(
            this,
            new object?[]
            {
                context,
                ignoreFilters,
                key,
                keys
            })!;

    async Task<object?> FindResult<T>(TDbContext context, bool ignoreFilters, IKey key, object[] keys)
        where T : class
    {
        var lambda = BuildLambda<T>(key.Properties, new(keys));
        IQueryable<T> set = context.Set<T>();
        if (ignoreFilters)
        {
            set = set.IgnoreQueryFilters();
        }

        return await set.SingleOrDefaultAsync(lambda);
    }
}
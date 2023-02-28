﻿namespace EfLocalDb;

public partial class SqlDatabase<TDbContext>
{
    /// <summary>
    ///     Calls <see cref="DbSet{TEntity}.FindAsync(object[])" /> on the <see cref="DbContext.Set{TEntity}()" /> for
    ///     <typeparamref name="T" />.
    /// </summary>
    public Task<T> Single<T>(Expression<Func<T, bool>>? predicate = null)
        where T : class
    {
        var set = Set<T>();

        return Single(set, predicate);
    }

    /// <summary>
    ///     Calls <see cref="DbSet{TEntity}.FindAsync(object[])" /> on the <see cref="DbContext.Set{TEntity}()" /> for
    ///     <typeparamref name="T" />.
    /// </summary>
    public Task<T> SingleIgnoreFilters<T>(Expression<Func<T, bool>>? predicate = null)
        where T : class
    {
        var set = Set<T>().IgnoreQueryFilters();

        return Single(set, predicate);
    }

    static Task<T> Single<T>(IQueryable<T> set, Expression<Func<T, bool>>? predicate)
        where T : class
    {
        if (predicate is null)
        {
            return set.SingleAsync();
        }

        return set.SingleAsync(predicate);
    }
}
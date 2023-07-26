namespace EfLocalDb;

public static partial class DbContextExtensions
{
    /// <summary>
    ///     Calls <see cref="DbContext.FindAsync(Type,object[])" /> on all entity types and returns true if the item exists.
    /// </summary>
    [Obsolete]
    public static Task<bool> Exists<TDbContext>(this TDbContext context, params object[] keys)
        where TDbContext : DbContext =>
        InnerExists(context, false, keys);

    /// <summary>
    ///     Calls <see cref="DbContext.FindAsync(Type,object[])" /> on all entity types and returns true if the item exists.
    /// </summary>
    [Obsolete]
    public static Task<bool> ExistsIgnoreFilter<TDbContext>(this TDbContext context, params object[] keys)
        where TDbContext : DbContext =>
        InnerExists(context, true, keys);

    static Task<bool> InnerExists<TDbContext>(TDbContext context, bool ignoreFilters, object[] keys)
        where TDbContext : DbContext
    {
        var database = DatabaseFromAnnotations(context);
        return database.InnerExists(context, ignoreFilters, keys);
    }
}
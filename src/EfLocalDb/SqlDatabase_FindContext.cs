namespace EfLocalDb;

public static class DbContextExtensions
{
    /// <summary>
    ///     Calls <see cref="DbContext.FindAsync(Type,object[])" /> on all entity types and returns all resulting items.
    /// </summary>
    public static Task<object> Find<TDbContext>(this TDbContext context, params object[] keys)
        where TDbContext : DbContext =>
        InnerFind(context, false, keys);

    /// <summary>
    ///     Calls <see cref="DbContext.FindAsync(Type,object[])" /> on all entity types and returns all resulting items.
    /// </summary>
    public static Task<object> FindIgnoreFilters<TDbContext>(this TDbContext context, params object[] keys)
        where TDbContext : DbContext =>
        InnerFind(context, true, keys);

    static Task<object> InnerFind<TDbContext>(this TDbContext context, bool ignoreFilters, object[] keys)
        where TDbContext : DbContext
    {
        var database = (SqlDatabase<TDbContext>) context.Model.FindRuntimeAnnotationValue("SqlDatabase")!;
        return database.InnerFind(ignoreFilters, keys);
    }
}
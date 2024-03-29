﻿namespace EfLocalDb;

public static partial  class DbContextExtensions
{
    /// <summary>
    ///     Calls <see cref="DbContext.FindAsync(Type,object[])" /> on all entity types and returns all resulting items.
    /// </summary>
    [Obsolete]
    public static Task<object> Find<TDbContext>(this TDbContext context, params object[] keys)
        where TDbContext : DbContext =>
        InnerFind(context, false, keys);

    /// <summary>
    ///     Calls <see cref="DbContext.FindAsync(Type,object[])" /> on all entity types and returns all resulting items.
    /// </summary>
    [Obsolete]
    public static Task<object> FindIgnoreFilters<TDbContext>(this TDbContext context, params object[] keys)
        where TDbContext : DbContext =>
        InnerFind(context, true, keys);

    static Task<object> InnerFind<TDbContext>(this TDbContext context, bool ignoreFilters, object[] keys)
        where TDbContext : DbContext
    {
        var database = DatabaseFromAnnotations(context);
        return database.InnerFind(context, ignoreFilters, keys);
    }

    static SqlDatabase<TDbContext> DatabaseFromAnnotations<TDbContext>(TDbContext context)
        where TDbContext : DbContext =>
        (SqlDatabase<TDbContext>) context.Model.FindRuntimeAnnotationValue("SqlDatabase")!;
}
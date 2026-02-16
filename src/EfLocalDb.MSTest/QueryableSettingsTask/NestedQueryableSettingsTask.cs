using Microsoft.EntityFrameworkCore.Query;

namespace EfLocalDb;

/// <summary>
///  Wraps <see cref="IIncludableQueryable{TEntity,TProperty}"/>
/// </summary>
public class IncludableQueryableSettingsTask<TEntity, TProperty> : SettingsTask
    where TEntity : class
{
    internal VerifySettings? Settings { get; }
    internal IIncludableQueryable<TEntity, TProperty> Inner { get; }
    internal Func<VerifySettings, IQueryable<TEntity>, Task<VerifyResult>> Query { get; }

    internal IncludableQueryableSettingsTask(IIncludableQueryable<TEntity, TProperty> source, VerifySettings? settings, Func<VerifySettings, IQueryable<TEntity>, Task<VerifyResult>> query)
        : base(settings, _ => query(_, source))
    {
        Inner = source;
        Query = query;
        Settings = settings;
    }
}

/// <summary>
/// Designed to mirror <see cref="EntityFrameworkQueryableExtensions"/>
/// https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.entityframeworkqueryableextensions
/// https://github.com/dotnet/efcore/blob/main/src/EFCore/Extensions/EntityFrameworkQueryableExtensions.cs
/// </summary>
public static class IncludableQueryableSettingsTaskExtensions
{
    /// <summary>
    /// https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.entityframeworkqueryableextensions.include?view=efcore-9.0#microsoft-entityframeworkcore-entityframeworkqueryableextensions-include-2(system-linq-iqueryable((-0))-system-linq-expressions-expression((system-func((-0-1)))))
    /// </summary>
    public static IncludableQueryableSettingsTask<TEntity, TProperty> Include<TEntity, TPreviousProperty, TProperty>(
        this IncludableQueryableSettingsTask<TEntity, TPreviousProperty> source,
        Expression<Func<TEntity, TProperty>> property)
        where TEntity : class =>
        new(source.Inner.Include(property), source.Settings, source.Query);

    /// <summary>
    /// https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.entityframeworkqueryableextensions.theninclude?view=efcore-9.0#microsoft-entityframeworkcore-entityframeworkqueryableextensions-theninclude-3(microsoft-entityframeworkcore-query-iincludablequeryable((-0-1))-system-linq-expressions-expression((system-func((-1-2)))))
    /// </summary>
    public static IncludableQueryableSettingsTask<TEntity, TProperty> ThenInclude<TEntity, TPreviousProperty, TProperty>(
        this IncludableQueryableSettingsTask<TEntity, TPreviousProperty> source,
        Expression<Func<TPreviousProperty, TProperty>> property)
        where TEntity : class =>
        new(source.Inner.ThenInclude(property), source.Settings, source.Query);

    /// <summary>
    /// https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.entityframeworkqueryableextensions.theninclude?view=efcore-9.0#microsoft-entityframeworkcore-entityframeworkqueryableextensions-theninclude-3(microsoft-entityframeworkcore-query-iincludablequeryable((-0-system-collections-generic-ienumerable((-1))))-system-linq-expressions-expression((system-func((-1-2)))))
    /// </summary>
    public static IncludableQueryableSettingsTask<TEntity, TProperty> ThenInclude<TEntity, TPreviousProperty, TProperty>(
        this IncludableQueryableSettingsTask<TEntity, List<TPreviousProperty>> source,
        Expression<Func<TPreviousProperty, TProperty>> property)
        where TEntity : class =>
        new(source.Inner.ThenInclude(property), source.Settings, source.Query);
}

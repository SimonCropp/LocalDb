using Microsoft.EntityFrameworkCore.Query;

namespace EfLocalDbNunit;

public class QueryableSettingsTask<TEntity>(IQueryable<TEntity> source,  VerifySettings? settings, Expression<Func<TEntity, bool>> expression, Func<VerifySettings,IQueryable<TEntity>, Task<VerifyResult>> buildTask)
    : SettingsTask(settings, verifySettings => buildTask(verifySettings, source))
    where TEntity : class
{
    readonly VerifySettings? settings = settings;

    public IncludeQueryableSettingsTask<TEntity, TProperty> Include<TProperty>(
        Expression<Func<TEntity, TProperty>> property) =>
        new(source.Include(property),settings, expression, buildTask);
}

public class IncludeQueryableSettingsTask<TEntity, TProperty>(IIncludableQueryable<TEntity, TProperty> source, VerifySettings? settings, Expression<Func<TEntity, bool>> expression, Func<VerifySettings,IQueryable<TEntity>, Task<VerifyResult>> buildTask)
    : SettingsTask(settings, verifySettings => buildTask(verifySettings, source))
    where TEntity : class
{
    readonly VerifySettings? settings = settings;

    public IncludeQueryableSettingsTask<TEntity, TNewProperty> Include<TNewProperty>(
        Expression<Func<TEntity, TNewProperty>> property) =>
        new(source.Include(property), settings, expression, buildTask);

    public IncludeQueryableSettingsTask<TEntity, TNewProperty> ThenInclude<TNewProperty>(
        Expression<Func<TProperty, TNewProperty>> property) =>
        new(source.ThenInclude(property), settings, expression, buildTask);
}
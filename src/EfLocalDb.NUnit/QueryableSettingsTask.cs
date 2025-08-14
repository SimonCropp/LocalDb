using Microsoft.EntityFrameworkCore.Query;

namespace EfLocalDbNunit;

public class QueryableSettingsTask<TEntity>(IQueryable<TEntity> source, string sourceFile, VerifySettings? settings, Expression<Func<TEntity, bool>> expression)
    : SettingsTask(settings, async verifySettings =>
    {
        using var verifier = BuildVerifier(sourceFile, verifySettings);
        return await verifier.Verify(source.SingleAsync(expression));
    })
    where TEntity : class
{
    readonly VerifySettings? settings = settings;

    public IncludeQueryableSettingsTask<TEntity, TProperty> Include<TProperty>(
        Expression<Func<TEntity, TProperty>> property) =>
        new(source.Include(property),sourceFile, settings, expression);
}

public class IncludeQueryableSettingsTask<TEntity, TProperty>(IIncludableQueryable<TEntity, TProperty> source, string sourceFile, VerifySettings? settings, Expression<Func<TEntity, bool>> expression)
    : SettingsTask(settings, async verifySettings =>
    {
        using var verifier = BuildVerifier(sourceFile, verifySettings);
        return await verifier.Verify(source.SingleAsync(expression));
    })
    where TEntity : class
{
    readonly VerifySettings? settings = settings;

    public IncludeQueryableSettingsTask<TEntity, TNewProperty> Include<TNewProperty>(
        Expression<Func<TEntity, TNewProperty>> property) =>
        new(source.Include(property), sourceFile, settings, expression);

    public IncludeQueryableSettingsTask<TEntity, TNewProperty> ThenInclude<TNewProperty>(
        Expression<Func<TProperty, TNewProperty>> property) =>
        new(source.ThenInclude(property), sourceFile, settings, expression);
}
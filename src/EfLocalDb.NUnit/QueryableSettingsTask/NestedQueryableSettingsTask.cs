using Microsoft.EntityFrameworkCore.Query;

namespace EfLocalDbNunit;

public class QueryableSettingsTask<TEntity, TProperty> : SettingsTask
    where TEntity : class
{
    VerifySettings? settings;
    IIncludableQueryable<TEntity, TProperty> source;
    Func<VerifySettings, IQueryable<TEntity>, Task<VerifyResult>> query;

    internal QueryableSettingsTask(IIncludableQueryable<TEntity, TProperty> source, VerifySettings? settings, Func<VerifySettings, IQueryable<TEntity>, Task<VerifyResult>> query)
        : base(settings, _ => query(_, source))
    {
        this.source = source;
        this.query = query;
        this.settings = settings;
    }

    public QueryableSettingsTask<TEntity, TNewProperty> Include<TNewProperty>(Expression<Func<TEntity, TNewProperty>> property) =>
        new(source.Include(property), settings, query);

    public QueryableSettingsTask<TEntity, TNewProperty> ThenInclude<TNewProperty>(Expression<Func<TProperty, TNewProperty>> property) =>
        new(source.ThenInclude(property), settings, query);
}
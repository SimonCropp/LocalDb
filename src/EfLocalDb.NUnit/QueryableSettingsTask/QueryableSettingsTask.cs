namespace EfLocalDbNunit;

/// <summary>
///  Wraps <see cref="IQueryable{TEntity}"/>
/// </summary>
public class QueryableSettingsTask<TEntity> : SettingsTask
    where TEntity : class
{
    VerifySettings? settings;
    IQueryable<TEntity> source;
    Func<VerifySettings, IQueryable<TEntity>, Task<VerifyResult>> query;

    internal QueryableSettingsTask(IQueryable<TEntity> source, VerifySettings? settings, Func<VerifySettings, IQueryable<TEntity>, Task<VerifyResult>> query)
        : base(settings, _ => query(_, source))
    {
        this.source = source;
        this.query = query;
        this.settings = settings;
    }

    public IncludableQueryableSettingsTask<TEntity, TProperty> Include<TProperty>(Expression<Func<TEntity, TProperty>> property) =>
        new(source.Include(property), settings, query);
}
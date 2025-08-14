namespace EfLocalDbNunit;

public abstract partial class LocalDbTestBase<T>
{
    [Pure]
    public virtual SettingsTask VerifyEntity<TEntity>(Guid id, [CallerFilePath] string sourceFile = "")
        where TEntity : class =>
        InnerVerifyEntity<TEntity>(id, sourceFile);

    [Pure]
    public virtual SettingsTask VerifyEntity<TEntity>(IQueryable<TEntity> entities, [CallerFilePath] string sourceFile = "") =>
        Verify(ResolveSingle(entities), sourceFile: sourceFile);

    static async Task<TEntity> ResolveSingle<TEntity>(IQueryable<TEntity> entities)
    {
        try
        {
            return await entities.SingleAsync();
        }
        catch (ObjectDisposedException exception)
        {
            throw NewDisposedException(exception);
        }
    }

    [Pure]
    public virtual SettingsTask VerifyEntities<TEntity>(IQueryable<TEntity> entities, [CallerFilePath] string sourceFile = "") =>
        Verify(ResolveList(entities), sourceFile: sourceFile);

    static async Task<List<TEntity>> ResolveList<TEntity>(IQueryable<TEntity> entities)
    {
        try
        {
            return await entities.ToListAsync();
        }
        catch (ObjectDisposedException exception)
        {
            throw NewDisposedException(exception);
        }
    }

    static Exception NewDisposedException(ObjectDisposedException exception) =>
        new("ObjectDisposedException while executing IQueryable. It is possible the IQueryable targets an ActData or ArrangeData that has already been cleaned up", exception);

    [Pure]
    public virtual SettingsTask VerifyEntity<TEntity>(long id, [CallerFilePath] string sourceFile = "")
        where TEntity : class =>
        InnerVerifyEntity<TEntity>(id, sourceFile);

    [Pure]
    public virtual SettingsTask VerifyEntity<TEntity>(int id, [CallerFilePath] string sourceFile = "")
        where TEntity : class =>
        InnerVerifyEntity<TEntity>(id, sourceFile);

    SettingsTask InnerVerifyEntity<TEntity>(object id, string sourceFile)
        where TEntity : class
    {
        var set = AssertData.Set<TEntity>();
        var entity = set.FindAsync(id);
        return Verify(entity, sourceFile: sourceFile);
    }
}
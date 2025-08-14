#pragma warning disable EF1001

using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Internal;
// ReSharper disable ExplicitCallerInfoArgument

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
        var primaryKey = set.EntityType.FindPrimaryKey()!;


        var values = new ValueBuffer([id]);
        var entityParameter = Expression.Parameter(typeof(TEntity));
        var predicate = ExpressionExtensions.BuildPredicate(primaryKey.Properties, values, entityParameter);
        var lambda = Expression.Lambda<Func<TEntity, bool>>(predicate, entityParameter);
        var func = ()=> set.SingleAsync(lambda);

        return Verify2<TEntity>(func, sourceFile: sourceFile);
    }
     static QueryableSettingsTask<TTarget> Verify2<TTarget>(
        Func<Task<TTarget>> target,
        VerifySettings? settings = null,
        [CallerFilePath] string sourceFile = "") =>
        Verify2<TTarget>(settings, sourceFile, _ => _.Verify(target()));

     static QueryableSettingsTask<TTarget> Verify2<TTarget>(
         VerifySettings? settings,
         string sourceFile,
         Func<InnerVerifier, Task<VerifyResult>> verify,
         bool useUniqueDirectory = false) =>
         new(
             settings,
             async verifySettings =>
             {
                 using var verifier = Verifier.BuildVerifier(sourceFile, verifySettings, useUniqueDirectory);
                 return await verify(verifier);
             });
}
namespace EfLocalDb;

public partial class SqlDatabase<TDbContext>
{
    public Task Remove(IEnumerable<object> entities) => RemoveInner(entities, Context);

    Task RemoveInner(IEnumerable<object> entities, TDbContext context)
    {
        foreach (var entity in ExpandEnumerable(entities))
        {
            context.Remove(entity);
        }

        return context.SaveChangesAsync();
    }

    public Task Remove(params object[] entities) => Remove((IEnumerable<object>) entities);

    public async Task RemoveUntracked(IEnumerable<object> entities)
    {
        await using var context = NewDbContext();
        await RemoveInner(entities, context);
    }

    public Task RemoveUntracked(params object[] entities) => RemoveUntracked((IEnumerable<object>) entities);
}
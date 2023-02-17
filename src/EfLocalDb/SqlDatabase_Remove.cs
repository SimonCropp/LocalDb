namespace EfLocalDb;

public partial class SqlDatabase<TDbContext>
{
    public Task RemoveData(IEnumerable<object> entities) => Remove(entities, Context);

    Task Remove(IEnumerable<object> entities, TDbContext context)
    {
        foreach (var entity in ExpandEnumerable(entities))
        {
            context.Remove(entity);
        }

        return context.SaveChangesAsync();
    }

    public Task RemoveData(params object[] entities) => RemoveData((IEnumerable<object>) entities);

    public async Task RemoveDataUntracked(IEnumerable<object> entities)
    {
        await using var context = NewDbContext();
        await Remove(entities, context);
    }

    public Task RemoveDataUntracked(params object[] entities) => RemoveDataUntracked((IEnumerable<object>) entities);
}
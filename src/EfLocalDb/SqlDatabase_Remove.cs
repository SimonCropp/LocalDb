namespace EfLocalDb;

public partial class SqlDatabase<TDbContext>
{
    public Task RemoveData(IEnumerable<object> entities, Cancel cancel = default) =>
        Context.RemoveData(entities, instance.EntityTypes, cancel);

    public Task RemoveData(params object[] entities) =>
        RemoveData((IEnumerable<object>) entities);

    public async Task RemoveDataUntracked(IEnumerable<object> entities, Cancel cancel = default)
    {
        await using var context = NewDbContext();
        await context.RemoveData(entities, instance.EntityTypes, cancel);
    }

    public Task RemoveDataUntracked(params object[] entities) => RemoveDataUntracked((IEnumerable<object>) entities);
}
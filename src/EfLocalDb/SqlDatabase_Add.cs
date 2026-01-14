namespace EfLocalDb;

public partial class SqlDatabase<TDbContext>
{
    public Task AddData(IEnumerable<object> entities, Cancel cancel = default) =>
        Context.AddData(entities, instance.EntityTypes, cancel);

    public Task AddData(params object[] entities) =>
        AddData((IEnumerable<object>) entities);

    public async Task AddDataUntracked(IEnumerable<object> entities, Cancel cancel = default)
    {
        await using var context = NewDbContext();
        await context.AddData(entities, instance.EntityTypes, cancel);
    }

    public Task AddDataUntracked(params object[] entities) => AddDataUntracked((IEnumerable<object>) entities);
}
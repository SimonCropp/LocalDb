namespace EfLocalDb;

public partial class SqlDatabase<TDbContext>
{
    public Task AddData(IEnumerable<object> entities) =>
        Context.AddData(entities, instance.EntityTypes);

    public Task AddData(params object[] entities) =>
        AddData((IEnumerable<object>) entities);

    public async Task AddDataUntracked(IEnumerable<object> entities)
    {
        await using var context = NewDbContext();
        await context.AddData(entities, instance.EntityTypes);
    }

    public Task AddDataUntracked(params object[] entities) => AddDataUntracked((IEnumerable<object>) entities);
}
namespace EfLocalDb;

public partial class SqlDatabase<TDbContext>
{
    public Task AddData(IEnumerable<object> entities) => Add(entities, Context);

    Task Add(IEnumerable<object> entities, TDbContext context)
    {
        foreach (var entity in ExpandEnumerable(entities))
        {
            context.Add(entity);
        }

        return context.SaveChangesAsync();
    }

    public Task AddData(params object[] entities) => AddData((IEnumerable<object>) entities);

    public async Task AddDataUntracked(IEnumerable<object> entities)
    {
        await using var context = NewDbContext();
        await Add(entities, context);
    }

    public Task AddDataUntracked(params object[] entities) => AddDataUntracked((IEnumerable<object>) entities);
}
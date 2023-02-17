namespace EfLocalDb;

public partial class SqlDatabase<TDbContext>
{
    public Task Add(IEnumerable<object> entities) => AddInner(entities, Context);

    Task AddInner(IEnumerable<object> entities, TDbContext context)
    {
        foreach (var entity in ExpandEnumerable(entities))
        {
            context.Add(entity);
        }

        return context.SaveChangesAsync();
    }

    public Task Add(params object[] entities) => Add((IEnumerable<object>) entities);

    public async Task AddUntracked(IEnumerable<object> entities)
    {
        await using var context = NewDbContext();
        await AddInner(entities, context);
    }

    public Task AddUntracked(params object[] entities) => AddUntracked((IEnumerable<object>) entities);
}
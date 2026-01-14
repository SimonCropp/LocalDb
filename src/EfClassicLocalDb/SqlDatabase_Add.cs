namespace EfLocalDb;

public partial class SqlDatabase<TDbContext>
{
    public Task AddData(IEnumerable<object> entities, Cancel cancel = default)
    {
        foreach (var entity in entities)
        {
            Context.Set(entity.GetType()).Add(entity);
        }

        return Context.SaveChangesAsync(cancel);
    }

    public Task AddData(params object[] entities) => AddData((IEnumerable<object>) entities);

    public async Task AddDataUntracked(IEnumerable<object> entities, Cancel cancel = default)
    {
        using var context = NewDbContext();
        foreach (var entity in entities)
        {
            context.Set(entity.GetType()).Add(entity);
        }

        await context.SaveChangesAsync(cancel);
    }

    public Task AddDataUntracked(params object[] entities) => AddDataUntracked((IEnumerable<object>) entities);
}
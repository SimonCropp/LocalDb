namespace EfLocalDb;

public partial class SqlDatabase<TDbContext>
{
    public Task AddData(IEnumerable<object> entities)
    {
        foreach (var entity in entities)
        {
            Context.Set(entity.GetType()).Add(entity);
        }

        return Context.SaveChangesAsync();
    }

    public Task AddData(params object[] entities) => AddData((IEnumerable<object>) entities);

    public async Task AddDataUntracked(IEnumerable<object> entities)
    {
        using var context = NewDbContext();
        foreach (var entity in entities)
        {
            Context.Set(entity.GetType()).Add(entity);
        }

        await context.SaveChangesAsync();
    }

    public Task AddDataUntracked(params object[] entities) => AddDataUntracked((IEnumerable<object>) entities);
}
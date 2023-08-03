namespace EfLocalDb;

public static partial class DbContextExtensions
{
    internal static IEnumerable<object> ExpandEnumerable(IEnumerable<object> entities, IReadOnlyList<IEntityType> entityTypes)
    {
        foreach (var entity in entities)
        {
            if (entity is IEnumerable enumerable)
            {
                var entityType = entity.GetType();
                if (entityTypes.Any(_ => _.ClrType != entityType))
                {
                    foreach (var nested in enumerable)
                    {
                        yield return nested;
                    }

                    continue;
                }
            }

            yield return entity;
        }
    }

    public static Task AddData<TDbContext>(this TDbContext context, IEnumerable<object> entities)
        where TDbContext : DbContext =>
        context.AddData(entities, context.Model.GetEntityTypes().ToArray());

    public static Task AddData<TDbContext>(this TDbContext context, params object[] entities)
        where TDbContext : DbContext =>
        context.AddData((IEnumerable<object>) entities);

    internal static Task AddData<TDbContext>(this TDbContext context, IEnumerable<object> entities, IReadOnlyList<IEntityType> entityTypes)
        where TDbContext : DbContext
    {
        foreach (var entity in ExpandEnumerable(entities, entityTypes))
        {
            context.Add(entity);
        }

        return context.SaveChangesAsync();
    }

    public static Task RemoveData<TDbContext>(this TDbContext context, IEnumerable<object> entities)
        where TDbContext : DbContext =>
        context.RemoveData(entities, context.Model.GetEntityTypes().ToArray());

    public static Task RemoveData<TDbContext>(this TDbContext context, params object[] entities)
        where TDbContext : DbContext =>
        context.RemoveData((IEnumerable<object>) entities);

    internal static Task RemoveData<TDbContext>(this TDbContext context, IEnumerable<object> entities, IReadOnlyList<IEntityType> entityTypes)
        where TDbContext : DbContext
    {
        foreach (var entity in ExpandEnumerable(entities, entityTypes))
        {
            context.Remove(entity);
        }

        return context.SaveChangesAsync();
    }
}
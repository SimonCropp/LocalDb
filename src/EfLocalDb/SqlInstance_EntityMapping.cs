namespace EfLocalDb;

public partial class SqlInstance<TDbContext>
    where TDbContext : DbContext
{
    record EntityKeyMap(Type[] KeyTypes, IKey Key, MethodInfo Find);
    Dictionary<Type, EntityKeyMap> entityKeyMap = [];
    public IReadOnlyList<IEntityType> EntityTypes { get; private set; } = null!;

    static MethodInfo findResult = typeof(SqlInstance<TDbContext>)
        .GetMethod("FindResult", BindingFlags.Static | BindingFlags.NonPublic)!;

    void InitEntityMapping()
    {
        EntityTypes = Model.GetEntityTypes().ToList();

        foreach (var entity in EntityTypes)
        {
            var clrType = entity.ClrType;

            // join entities ClrTypes are dictionaries
            if (clrType.Assembly.FullName!.StartsWith("System"))
            {
                continue;
            }

            if (entity.IsOwned())
            {
                continue;
            }

            var key = entity.FindPrimaryKey();
            if (key is null)
            {
                continue;
            }

            var find = findResult.MakeGenericMethod(clrType);
            var keyTypes = key.Properties.Select(_ => _.ClrType).ToArray();
            entityKeyMap.Add(clrType, new(keyTypes, key, find));
        }
    }

    internal IKey FindKey<T>(object[] keys, out MethodInfo find)
        where T : class
    {
        (var keyTypes, var key, find) = entityKeyMap[typeof(T)];

        if (KeyTypesMatch(keyTypes, keys))
        {
            return key;
        }

        throw new("Key types dont match");
    }

    IEnumerable<(IKey key, MethodInfo find)> FindKeys(object[] keys)
    {
        foreach (var (keyTypes, key, find) in entityKeyMap.Values)
        {
            if (KeyTypesMatch(keyTypes, keys))
            {
                yield return new(key, find);
            }
        }
    }

    static bool KeyTypesMatch(Type[] keyTypes, object[] keys)
    {
        if (keyTypes.Length != keys.Length)
        {
            return false;
        }

        for (var i = 0; i < keyTypes.Length; i++)
        {
            if (keyTypes[i] != keys[i].GetType())
            {
                return false;
            }
        }

        return true;
    }

    internal static Task<object?> InvokeFind(TDbContext context, bool ignoreFilters, object[] keys, MethodInfo find, IKey key) =>
        (Task<object?>) find.Invoke(
            null,
            [
                context,
                ignoreFilters,
                key,
                keys
            ])!;

    static async Task<object?> FindResult<T>(TDbContext context, bool ignoreFilters, IKey key, object[] keys)
        where T : class
    {
        var lambda = Lambda<T>.Build(key.Properties, new(keys));
        IQueryable<T> set = context.Set<T>();
        if (ignoreFilters)
        {
            set = set.IgnoreQueryFilters();
        }

        return await set.SingleOrDefaultAsync(lambda);
    }

    internal async Task<List<object>> FindResults(TDbContext context, bool ignoreFilters, object[] keys)
    {
        var list = new List<object>();

        foreach (var (key, find) in FindKeys(keys))
        {
            var result = await InvokeFind(context, ignoreFilters, keys, find, key);
            if (result is not null)
            {
                list.Add(result);
            }
        }

        return list;
    }
}
#if EF
namespace EfLocalDb;
#else
namespace LocalDb;
#endif

public delegate string BuildConnection(string instance, string database);

public static class LocalDbSettings
{
    internal static BuildConnection connectionBuilder = (instance, database) =>
    {
        // Pool master/template connections (reused frequently), but not per-test databases (unique, used once)
        var pooling = database is "master" or "template";
        return $"Data Source=(LocalDb)\\{instance};Database={database};Pooling={pooling}";
    };

    public static void ConnectionBuilder(BuildConnection builder) => connectionBuilder = builder;
}

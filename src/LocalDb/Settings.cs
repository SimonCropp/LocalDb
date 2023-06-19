#if EF
namespace EfLocalDb;
#else
namespace LocalDb;
#endif

public delegate string BuildConnection(string instance, string database);

public static class LocalDbSettings
{
    internal static BuildConnection connectionBuilder = (instance, database) =>
        $"Data Source=(LocalDb)\\{instance};Database={database};Pooling=true";

    public static void ConnectionBuilder(BuildConnection builder) => connectionBuilder = builder;
}

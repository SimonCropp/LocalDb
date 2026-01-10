#if EF
namespace EfLocalDb;
#else
namespace LocalDb;
#endif

public static class LocalDbSettings
{
    internal static Action<SqlConnectionStringBuilder>? connectionBuilder;

    public static void ConnectionBuilder(Action<SqlConnectionStringBuilder> builder) =>
        connectionBuilder = builder;

    internal static string BuildConnectionString(string instance, string database, bool pool)
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = $"(LocalDb)\\{instance}",
            InitialCatalog = database,
            Pooling = pool
        };
        connectionBuilder?.Invoke(builder);
        return builder.ConnectionString;
    }
}

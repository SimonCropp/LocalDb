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

    /// <summary>
    /// The number of seconds LocalDB waits before shutting down after the last connection closes.
    /// Can be configured via the <c>LocalDBShutdownTimeout</c> environment variable.
    /// Defaults to 5 minutes.
    /// </summary>
    public static ushort ShutdownTimeout { get; set; } = ResolveShutdownTimeout();

    /// <summary>
    /// Controls whether databases are automatically taken offline when disposed.
    /// Can be configured via the <c>LocalDBAutoOffline</c> environment variable ("true" or "false").
    /// When null (default), automatically enables offline mode if a CI environment is detected.
    /// </summary>
    public static bool? DBAutoOffline { get; set; } = ResolveDBAutoOffline();

    static ushort ResolveShutdownTimeout()
    {
        var envValue = Environment.GetEnvironmentVariable("LocalDBShutdownTimeout");
        if (envValue is null)
        {
            return 500;
        }

        if (ushort.TryParse(envValue, out var timeout))
        {
            return timeout;
        }

        throw new ArgumentException($"Failed to parse LocalDBShutdownTimeout environment variable: {envValue}");
    }

    static bool? ResolveDBAutoOffline()
    {
        var envValue = Environment.GetEnvironmentVariable("LocalDBAutoOffline");
        return envValue switch
        {
            "true" => true,
            "false" => false,
            _ => null
        };
    }
}

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

    /// <summary>
    /// How long an instance directory must be untouched before automatic cleanup removes the
    /// instance and the directory LocalDB keeps for it. This reclaims instances whose data
    /// directory is gone, which the per run cleanup can no longer see.
    /// Can be configured via the <c>LocalDBInstanceCleanupDays</c> environment variable.
    /// Defaults to 30 days. Set to <see cref="TimeSpan.Zero" /> to disable.
    /// </summary>
    public static TimeSpan InstanceCleanupThreshold { get; set; } = ResolveInstanceCleanupThreshold();

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

    static TimeSpan ResolveInstanceCleanupThreshold()
    {
        var envValue = Environment.GetEnvironmentVariable("LocalDBInstanceCleanupDays");
        if (envValue is null)
        {
            return TimeSpan.FromDays(30);
        }

        if (ushort.TryParse(envValue, out var days))
        {
            return TimeSpan.FromDays(days);
        }

        throw new ArgumentException($"Failed to parse LocalDBInstanceCleanupDays environment variable: {envValue}");
    }
}

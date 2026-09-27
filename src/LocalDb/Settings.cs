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
    /// The number of minutes LocalDB waits before shutting down after the last connection closes.
    /// Maps to the <c>user instance timeout</c> server option, which accepts 5 to 65535.
    /// Can be configured via the <c>LocalDBShutdownTimeout</c> environment variable.
    /// Always 5 minutes on CI. Otherwise defaults to 5 minutes when an AI CLI is detected, and 10 minutes when not.
    /// </summary>
    public static ushort ShutdownTimeout { get; set; } = ResolveShutdownTimeout();

    /// <summary>
    /// How long an instance directory must be untouched before automatic cleanup removes the
    /// instance and the directory LocalDB keeps for it. This reclaims instances whose data
    /// directory is gone, which the per run cleanup can no longer see.
    /// Can be configured via the <c>LocalDBInstanceCleanupDays</c> environment variable.
    /// Defaults to 30 days. Set to <see cref="TimeSpan.Zero" /> to disable.
    /// </summary>
    public static TimeSpan InstanceCleanupThreshold { get; set; } = ResolveInstanceCleanupThreshold();

    /// <summary>
    /// The number of databases in the pool used by pooled tests. Each pooled test leases one
    /// database for the duration of the test and rolls its changes back on release, so this
    /// caps how many pooled tests can run concurrently.
    /// Can be configured via the <c>LocalDBPoolSize</c> environment variable.
    /// Defaults to <see cref="Environment.ProcessorCount" />.
    /// </summary>
    public static ushort PoolSize { get; set; } = ResolvePoolSize();

    static ushort ResolvePoolSize()
    {
        var envValue = Environment.GetEnvironmentVariable("LocalDBPoolSize");
        if (envValue is null)
        {
            return (ushort) Math.Clamp(Environment.ProcessorCount, 1, ushort.MaxValue);
        }

        if (ushort.TryParse(envValue, out var size) &&
            size > 0)
        {
            return size;
        }

        throw new ArgumentException($"Failed to parse LocalDBPoolSize environment variable: {envValue}");
    }

    static ushort ResolveShutdownTimeout()
    {
        // A CI agent runs one build then idles, so there is no later run to keep the instance warm for
        if (IsCI)
        {
            return 5;
        }

        var envValue = Environment.GetEnvironmentVariable("LocalDBShutdownTimeout");
        if (envValue is null)
        {
            if (AiCliDetector.Detected)
            {
                return 5;
            }

            return 10;
        }

        if (ushort.TryParse(envValue, out var timeout))
        {
            return timeout;
        }

        throw new ArgumentException($"Failed to parse LocalDBShutdownTimeout environment variable: {envValue}");
    }

    // Build servers as detected by DiffEngine's BuildServerDetector, less Docker and WSL, which are
    // dev environments. The generic CI variable also covers CircleCI and others that set it.
    internal static bool IsCI { get; } = DetectCI(Environment.GetEnvironmentVariable);

    internal static bool DetectCI(Func<string, string?> variable) =>
        IsTrue(variable("CI")) ||
        variable("GITHUB_ACTIONS") is not null ||
        IsTrue(variable("TF_BUILD")) ||
        variable("APPVEYOR") is not null ||
        variable("TEAMCITY_VERSION") is not null ||
        variable("JENKINS_URL") is not null ||
        variable("GITLAB_CI") is not null ||
        variable("TRAVIS_BUILD_ID") is not null ||
        variable("BITBUCKET_BUILD_NUMBER") is not null ||
        variable("GO_SERVER_URL") is not null ||
        string.Equals(variable("BuildRunner"), "MyGet", StringComparison.OrdinalIgnoreCase);

    // AppVeyor and Azure DevOps use "True", others "true" or "1"
    static bool IsTrue(string? value)
    {
        if (value == "1")
        {
            return true;
        }

        return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
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

[TestFixture]
public class LocalDbSettingsTests
{
    [Test]
    public void ShutdownTimeout_DefaultsTo500()
    {
        // If LocalDBShutdownTimeout env var is not set, default is 500
        var envValue = Environment.GetEnvironmentVariable("LocalDBShutdownTimeout");
        if (envValue is null)
        {
            That(LocalDbSettings.ShutdownTimeout, Is.EqualTo(500));
        }
    }

    [Test]
    public void ShutdownTimeout_ReflectsEnvironmentVariable()
    {
        var envValue = Environment.GetEnvironmentVariable("LocalDBShutdownTimeout");
        if (envValue is not null && ushort.TryParse(envValue, out var expected))
        {
            That(LocalDbSettings.ShutdownTimeout, Is.EqualTo(expected));
        }
    }

    [Test]
    public void ShutdownTimeout_CanBeSetProgrammatically()
    {
        var original = LocalDbSettings.ShutdownTimeout;
        try
        {
            LocalDbSettings.ShutdownTimeout = 60;
            That(LocalDbSettings.ShutdownTimeout, Is.EqualTo(60));
        }
        finally
        {
            LocalDbSettings.ShutdownTimeout = original;
        }
    }

    [Test]
    public void DBAutoOffline_DefaultsToNullWhenEnvVarNotSet()
    {
        // If LocalDBAutoOffline env var is not set, default is null
        var envValue = Environment.GetEnvironmentVariable("LocalDBAutoOffline");
        if (envValue is null)
        {
            That(LocalDbSettings.DBAutoOffline, Is.Null);
        }
    }

    [Test]
    public void DBAutoOffline_ReflectsEnvironmentVariable()
    {
        var envValue = Environment.GetEnvironmentVariable("LocalDBAutoOffline");
        if (envValue == "true")
        {
            That(LocalDbSettings.DBAutoOffline, Is.True);
        }
        else if (envValue == "false")
        {
            That(LocalDbSettings.DBAutoOffline, Is.False);
        }
    }

    [Test]
    public void DBAutoOffline_CanBeSetProgrammatically()
    {
        var original = LocalDbSettings.DBAutoOffline;
        try
        {
            LocalDbSettings.DBAutoOffline = true;
            That(LocalDbSettings.DBAutoOffline, Is.True);

            LocalDbSettings.DBAutoOffline = false;
            That(LocalDbSettings.DBAutoOffline, Is.False);

            LocalDbSettings.DBAutoOffline = null;
            That(LocalDbSettings.DBAutoOffline, Is.Null);
        }
        finally
        {
            LocalDbSettings.DBAutoOffline = original;
        }
    }

    // the default cannot be asserted here: the module initializer sets this to zero so the
    // suite does not sweep the real instance root
    [Test]
    public void InstanceCleanupThreshold_CanBeSetProgrammatically()
    {
        var original = LocalDbSettings.InstanceCleanupThreshold;
        try
        {
            LocalDbSettings.InstanceCleanupThreshold = TimeSpan.FromDays(7);
            That(LocalDbSettings.InstanceCleanupThreshold, Is.EqualTo(TimeSpan.FromDays(7)));
        }
        finally
        {
            LocalDbSettings.InstanceCleanupThreshold = original;
        }
    }
}

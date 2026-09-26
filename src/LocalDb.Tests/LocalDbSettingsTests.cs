[TestFixture]
public class LocalDbSettingsTests
{
    [Test]
    public void ShutdownTimeout_Default()
    {
        // If LocalDBShutdownTimeout env var is not set, default is 5 minutes for AI and 10 otherwise
        var envValue = Environment.GetEnvironmentVariable("LocalDBShutdownTimeout");
        if (envValue is null)
        {
            var expected = AiCliDetector.Detected ? 5 : 10;
            That(LocalDbSettings.ShutdownTimeout, Is.EqualTo(expected));
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

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
}

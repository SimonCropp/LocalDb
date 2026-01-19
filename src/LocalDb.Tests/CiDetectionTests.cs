[TestFixture]
public class CiDetectionTests
{
    [Test]
    public void ResolveDbAutoOffline_ExplicitTrue_ReturnsTrue()
    {
        var result = CiDetection.ResolveDbAutoOffline(true);
        That(result, Is.True);
    }

    [Test]
    public void ResolveDbAutoOffline_ExplicitFalse_ReturnsFalse()
    {
        var result = CiDetection.ResolveDbAutoOffline(false);
        That(result, Is.False);
    }

    [Test]
    public void ResolveDbAutoOffline_Null_RespectsEnvironmentVariableAndCIDetection()
    {
        var localDbAutoOfflineEnv = Environment.GetEnvironmentVariable("LocalDBAutoOffline");
        var expectedFromEnv = localDbAutoOfflineEnv switch
        {
            "true" => true,
            "false" => false,
            _ => (bool?)null
        };

        var result = CiDetection.ResolveDbAutoOffline(null);
        That(result, Is.EqualTo(expectedFromEnv ?? CiDetection.IsCI));
    }

    [Test]
    public void IsCI_ReflectsEnvironmentVariables()
    {
        var expected =
            Environment.GetEnvironmentVariable("CI") is "true" or "1" ||
            Environment.GetEnvironmentVariable("TF_BUILD") == "True" ||
            Environment.GetEnvironmentVariable("TEAMCITY_VERSION") is not null ||
            Environment.GetEnvironmentVariable("JENKINS_URL") is not null ||
            Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true";
        That(CiDetection.IsCI, Is.EqualTo(expected));
    }

    [Test]
    public void LocalDBAutoOffline_ParsesEnvironmentVariable()
    {
        var envValue = Environment.GetEnvironmentVariable("LocalDBAutoOffline");
        var expected = envValue switch
        {
            "true" => true,
            "false" => false,
            _ => (bool?)null
        };

        // When LocalDBAutoOffline is set, it should override CI detection
        // When not set, CI detection should be used
        var result = CiDetection.ResolveDbAutoOffline(null);
        That(result, Is.EqualTo(expected ?? CiDetection.IsCI));
    }

    [Test]
    public void ResolveDbAutoOffline_ExplicitParameter_OverridesEnvironmentVariable()
    {
        // Explicit parameter should always take precedence over environment variable
        That(CiDetection.ResolveDbAutoOffline(true), Is.True);
        That(CiDetection.ResolveDbAutoOffline(false), Is.False);
    }
}

[TestFixture]
public class LocalDbSettingsTests
{
    [Test]
    public void ShutdownTimeout_Default()
    {
        // CI is always 5 minutes. Otherwise, if LocalDBShutdownTimeout env var is not set, default is 5 minutes for AI and 10 otherwise
        if (LocalDbSettings.IsCI)
        {
            That(LocalDbSettings.ShutdownTimeout, Is.EqualTo(5));
            return;
        }

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
        if (!LocalDbSettings.IsCI && envValue is not null && ushort.TryParse(envValue, out var expected))
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

    [TestCase("CI", "true")]
    [TestCase("CI", "True")]
    [TestCase("CI", "1")]
    [TestCase("GITHUB_ACTIONS", "true")]
    [TestCase("TF_BUILD", "True")]
    [TestCase("APPVEYOR", "True")]
    [TestCase("TEAMCITY_VERSION", "2025.1")]
    [TestCase("JENKINS_URL", "http://jenkins")]
    [TestCase("GITLAB_CI", "true")]
    [TestCase("TRAVIS_BUILD_ID", "123")]
    [TestCase("BITBUCKET_BUILD_NUMBER", "123")]
    [TestCase("GO_SERVER_URL", "https://gocd")]
    [TestCase("BuildRunner", "MyGet")]
    public void DetectCI_Detected(string name, string value) =>
        That(LocalDbSettings.DetectCI(_ =>
        {
            if (_ == name)
            {
                return value;
            }

            return null;
        }), Is.True);

    [TestCase("CI", "false")]
    [TestCase("CI", "0")]
    [TestCase("TF_BUILD", "False")]
    [TestCase("BuildRunner", "Local")]
    [TestCase("DOTNET_RUNNING_IN_CONTAINER", "true")]
    [TestCase("WSL_DISTRO_NAME", "Ubuntu")]
    public void DetectCI_NotDetected(string name, string value) =>
        That(LocalDbSettings.DetectCI(_ =>
        {
            if (_ == name)
            {
                return value;
            }

            return null;
        }), Is.False);

    [Test]
    public void DetectCI_NoVariables() =>
        That(LocalDbSettings.DetectCI(_ => null), Is.False);

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

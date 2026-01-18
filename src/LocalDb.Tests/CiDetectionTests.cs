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
    public void ResolveDbAutoOffline_Null_ReturnsIsCI()
    {
        var result = CiDetection.ResolveDbAutoOffline(null);
        That(result, Is.EqualTo(CiDetection.IsCI));
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
}

static class CiDetection
{
    public static bool IsCI { get; } =
        Environment.GetEnvironmentVariable("CI") is "true" or "1" ||
        Environment.GetEnvironmentVariable("TF_BUILD") == "True" ||
        Environment.GetEnvironmentVariable("TEAMCITY_VERSION") is not null ||
        Environment.GetEnvironmentVariable("JENKINS_URL") is not null ||
        Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true";

    static bool? LocalDBAutoOffline { get; } =
        Environment.GetEnvironmentVariable("LocalDBAutoOffline") switch
        {
            "true" => true,
            "false" => false,
            _ => null
        };

    public static bool ResolveDbAutoOffline(bool? dbAutoOffline) =>
        dbAutoOffline ?? LocalDBAutoOffline ?? IsCI;
}

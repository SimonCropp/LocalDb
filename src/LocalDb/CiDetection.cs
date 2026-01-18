static class CiDetection
{
    public static bool IsCI { get; } = Environment.GetEnvironmentVariable("CI") is not null;

    public static bool ResolveDbAutoOffline(bool? dbAutoOffline) =>
        dbAutoOffline ?? IsCI;
}

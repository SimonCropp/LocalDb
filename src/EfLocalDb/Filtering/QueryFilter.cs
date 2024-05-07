namespace EfLocalDb;

public static class QueryFilter
{
    static AsyncLocal<bool> disabled = new();

    public static void Disable() =>
        disabled.Value = true;

    public static void Enable() =>
        disabled.Value = false;

    public static bool IsEnabled => !disabled.Value;
    public static bool IsDisabled => disabled.Value;
}
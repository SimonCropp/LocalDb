static class CombinationCallback
{
    internal static void SetInstance(ILocalDbTestBase value) =>
        instance.Value = value;

    static AsyncLocal<ILocalDbTestBase?> instance = new();

    [ModuleInitializer]
    public static void Init() =>
        CombinationSettings.UseCallbacks(
            _ => Task.CompletedTask,
            (_, _) =>
            {
                if (instance.Value == null)
                {
                    return Task.CompletedTask;
                }

                return instance.Value.Reset();
            },
            (_, _) => Task.CompletedTask);
}
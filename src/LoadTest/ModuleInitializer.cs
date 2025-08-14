[assembly: NonParallelizable]
[assembly: LevelOfParallelism(1)]

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        LocalDbLogging.EnableVerbose();
        LocalDbSettings.ConnectionBuilder((instance, database) => $"Data Source=(LocalDb)\\{instance};Database={database};Pooling=true;Connection Timeout=300");
    }
}
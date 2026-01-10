public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        LocalDbLogging.EnableVerbose();
        LocalDbSettings.ConnectionBuilder(_ => _.ConnectTimeout = 300);
    }
}
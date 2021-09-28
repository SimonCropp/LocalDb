#region MethodTimeLogger
static class MethodTimeLogger
{
    public static void Log(MethodBase method, long milliseconds, string? message)
    {
        if (!LocalDbLogging.Enabled)
        {
            return;
        }
        if (message is null)
        {
            LocalDbLogging.Log($"{method.Name} {milliseconds}ms");
            return;
        }

        LocalDbLogging.Log($"{method.Name} {milliseconds}ms {message}");
    }
}
#endregion
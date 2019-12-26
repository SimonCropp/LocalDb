using System.Diagnostics;
using System.Reflection;

#region MethodTimeLogger
static class MethodTimeLogger
{
    public static void Log(MethodBase method, long milliseconds, string message)
    {
        if (!LocalDbLogging.Enabled)
        {
            return;
        }
        if (message == null)
        {
            Trace.WriteLine($"{method.Name} {milliseconds}ms", "LocalDb");
            return;
        }

        Trace.WriteLine($"{method.Name} {milliseconds}ms {message}", "LocalDb");
    }
}
#endregion
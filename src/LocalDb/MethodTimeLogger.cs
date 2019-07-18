using System.Diagnostics;
using System.Reflection;

#region MethodTimeLogger
static class MethodTimeLogger
{
    public static void Log(MethodBase methodBase, long milliseconds, string message)
    {
        if (LocalDbLogging.Enabled)
        {
            Trace.WriteLine($"{methodBase.Name} {milliseconds}ms", "LocalDb");
        }
    }
}
#endregion
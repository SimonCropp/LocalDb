using System.Diagnostics;
using System.Reflection;
#if EF
using EfLocalDb;
#else
using LocalDb;
#endif

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
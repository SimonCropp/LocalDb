using System.Diagnostics;
#if EF
namespace EfLocalDb
#else
namespace LocalDb
#endif
{
    public static class Logging
    {
        public static void EnableVerbose()
        {
            VerboseLogging = true;
        }

        public static bool VerboseLogging { get; private set; }

        public static void Log(string message)
        {
            if (VerboseLogging)
            {
                Trace.WriteLine(message, "LocalDb");
            }
        }
    }
}
using System.Diagnostics;

#if EF
namespace EfLocalDb
#else
namespace LocalDb
#endif
{
    public static class Logging
    {
        public static void Enable()
        {
            Enabled = true;
        }

        public static bool Enabled { get; private set; }
        public static void Log(string message)
        {
            if (Enabled)
            {
                Trace.WriteLine(message, "LocalDb");
            }
        }
    }
}
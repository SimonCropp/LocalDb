using System.Diagnostics;

#if EF
namespace EfLocalDb
#else
namespace LocalDb
#endif
{
    #region LocalDbLogging
    /// <summary>
    /// Controls the logging level.
    /// </summary>
    public static class LocalDbLogging
    {
        /// <summary>
        /// Enable verbose logging to <see cref="Trace.WriteLine(string)"/>
        /// </summary>
        public static void EnableVerbose()
        {
            Enabled = true;
        }

        internal static bool Enabled;

        internal static void Log(string message)
        {
            if (Enabled)
            {
                Trace.WriteLine(message, "LocalDb");
            }
        }
    }
    #endregion
}
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
    }
}
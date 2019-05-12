using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace EFLocalDb
{
    public abstract class LocalDbTestBase<T>
        where T : DbContext
    {
        /// <summary>
        ///   Build DB with a name based on the calling Method
        /// </summary>
        /// <param name="suffix">Text based on the inline data to make the db name unique.
        /// Used for parameterized tests that need to run in parallel (eg xUnit [Theory] or XUnit [TestCase]).
        /// </param>
        /// <param name="memberName">Used to make the db name unique per method. Will default to the caller method name is used.</param>
        public Task<LocalDb<T>> LocalDb(
            string suffix = null,
            [CallerMemberName] string memberName = null)
        {
            return LocalDb<T>.Build(this, suffix, memberName);
        }
    }
}
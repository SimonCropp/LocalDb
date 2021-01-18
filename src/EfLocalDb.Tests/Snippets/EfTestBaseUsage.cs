using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using EfLocalDb;
using Xunit;

namespace TestBase
{
    #region EfTestBase

    public class TestBase
    {
        static SqlInstance<TheDbContext> sqlInstance;

        static TestBase()
        {
            sqlInstance = new(
                constructInstance: builder => new(builder.Options));
        }

        public Task<SqlDatabase<TheDbContext>> LocalDb(
            [CallerFilePath] string testFile = "",
            string? databaseSuffix = null,
            [CallerMemberName] string memberName = "")
        {
            return sqlInstance.Build(testFile, databaseSuffix, memberName);
        }
    }

    public class Tests :
        TestBase
    {
        [Fact]
        public async Task Test()
        {
            await using var database = await LocalDb();
            TheEntity entity = new()
            {
                Property = "prop"
            };
            await database.AddData(entity);

            Assert.Single(database.Context.TestEntities);
        }
    }

    #endregion
}
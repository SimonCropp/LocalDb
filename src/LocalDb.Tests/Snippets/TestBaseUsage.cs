using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using LocalDb;
using Xunit;

namespace TestBase
{
    #region TestBase

    public abstract class TestBase
    {
        static SqlInstance instance;

        static TestBase()
        {
            instance = new(
                name:"TestBaseUsage",
                buildTemplate: TestDbBuilder.CreateTable);
        }

        public Task<SqlDatabase> LocalDb(
            string? databaseSuffix = null,
            [CallerMemberName] string memberName = "")
        {
            return instance.Build(GetType().Name, databaseSuffix, memberName);
        }
    }

    public class Tests:
        TestBase
    {
        [Fact]
        public async Task Test()
        {
            await using var database = await LocalDb();
            await TestDbBuilder.AddData(database.Connection);
            Assert.Single(await TestDbBuilder.GetData(database.Connection));
        }
    }

    #endregion
}
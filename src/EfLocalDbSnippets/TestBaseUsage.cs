using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using EfLocalDb;
using Xunit;

namespace TestBase
{
    #region EfTestBase

    public class TestBase
    {
        static SqlInstance<TheDbContext> instance;

        static TestBase()
        {
            instance = new SqlInstance<TheDbContext>(
                buildTemplate: (connection, builder) =>
                {
                    using (var dbContext = new TheDbContext(builder.Options))
                    {
                        dbContext.Database.EnsureCreated();
                    }
                },
                constructInstance: builder => new TheDbContext(builder.Options));
        }

        public Task<SqlDatabase<TheDbContext>> LocalDb(
            string databaseSuffix = null,
            [CallerMemberName] string memberName = null)
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
            var database = await LocalDb();
            using (var dbContext = database.NewDbContext())
            {
                var entity = new TheEntity
                {
                    Property = "prop"
                };
                dbContext.Add(entity);
                dbContext.SaveChanges();
            }

            using (var dbContext = database.NewDbContext())
            {
                Assert.Single(dbContext.TestEntities);
            }
        }
    }

    #endregion
}
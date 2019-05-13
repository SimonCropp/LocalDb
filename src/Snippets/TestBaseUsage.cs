using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using EFLocalDb;
using Xunit;

namespace TestBase
{
    #region TestBase

    public class TestBase
    {
        static SqlInstance<TheDbContext> instance;

        static TestBase()
        {
            instance = new SqlInstance<TheDbContext>(
                (connection, builder) =>
                {
                    using (var dbContext = new TheDbContext(builder.Options))
                    {
                        dbContext.Database.EnsureCreated();
                    }
                },
                builder => new TheDbContext(builder.Options));
        }

        public Task<SqlDatabase<TheDbContext>> LocalDb(
            string databaseSuffix = null,
            [CallerMemberName] string memberName = null)
        {
            return instance.Build(this, databaseSuffix, memberName);
        }
    }

    public class Tests:
        TestBase
    {
        [Fact]
        public async Task Test()
        {
            var localDb = await LocalDb();
            using (var dbContext = localDb.NewDbContext())
            {
                var entity = new TestEntity
                {
                    Property = "prop"
                };
                dbContext.Add(entity);
                dbContext.SaveChanges();
            }

            using (var dbContext = localDb.NewDbContext())
            {
                Assert.Single(dbContext.TestEntities);
            }
        }
    }

    #endregion
}
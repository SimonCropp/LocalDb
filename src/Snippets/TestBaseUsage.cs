using System.Threading.Tasks;
using EFLocalDb;
using Xunit;

namespace TestBase
{
    #region TestBase

    public class TestBase
    {
        static TestBase()
        {
            LocalDb<TheDbContext>.Register(
                (connection, builder) =>
                {
                    using (var dbContext = new TheDbContext(builder.Options))
                    {
                        dbContext.Database.EnsureCreated();
                    }
                },
                builder => new TheDbContext(builder.Options));
        }
    }

    public class Tests:
        TestBase
    {
        [Fact]
        public async Task Test()
        {
            var localDb = await LocalDb<TheDbContext>.Build(this);
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
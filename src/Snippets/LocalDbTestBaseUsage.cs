using System.Threading.Tasks;
using EFLocalDb;
using Xunit;

namespace LocalDbTestBaseUsage
{
    #region LocalDbTestBaseUsage
    
    public class MyTestBase:
        LocalDbTestBase<TheDbContext>
    {
        static MyTestBase()
        {
            LocalDb<TheDbContext>.Register(
                (connection, optionsBuilder) =>
                {
                    using (var dbContext = new TheDbContext(optionsBuilder.Options))
                    {
                        dbContext.Database.EnsureCreated();
                    }
                },
                builder => new TheDbContext(builder.Options));
        }
    }

    public class Tests:
        MyTestBase
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
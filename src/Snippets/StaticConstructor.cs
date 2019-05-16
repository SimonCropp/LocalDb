using System.Threading.Tasks;
using EFLocalDb;
using Xunit;

namespace StaticConstructor
{
    #region StaticConstructor

    public class Tests
    {
        static Tests()
        {
            LocalDb<TheDbContext>.Register(
                buildTemplate: (connection, builder) =>
                {
                    using (var dbContext = new TheDbContext(builder.Options))
                    {
                        dbContext.Database.EnsureCreated();
                    }
                },
                constructInstance: builder => new TheDbContext(builder.Options));
        }

        [Fact]
        public async Task Test()
        {
            var localDb = await LocalDb<TheDbContext>.Build();
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
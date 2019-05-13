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
            StaticInstance<TheDbContext>.Register(
                (connection, optionsBuilder) =>
                {
                    using (var dbContext = new TheDbContext(optionsBuilder.Options))
                    {
                        dbContext.Database.EnsureCreated();
                    }
                },
                builder => new TheDbContext(builder.Options));
        }

        [Fact]
        public async Task Test()
        {
            var localDb = await StaticInstance<TheDbContext>.Build(this);
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
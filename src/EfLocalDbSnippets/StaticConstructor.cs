using System.Threading.Tasks;
using EfLocalDb;
using Xunit;

namespace StaticConstructor
{
    #region EfStaticConstructor

    public class Tests
    {
        static SqlInstance<TheDbContext> sqlInstance;

        static Tests()
        {
            sqlInstance = new SqlInstance<TheDbContext>(
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
            var database = await sqlInstance.Build();
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
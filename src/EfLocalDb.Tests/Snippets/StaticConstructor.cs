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
            sqlInstance = new(
                builder => new(builder.Options));
        }

        public async Task Test()
        {
            TheEntity entity = new()
            {
                Property = "prop"
            };
            List<object> data = new() {entity};
            await using var database = await sqlInstance.Build(data);
            Assert.Single(database.Context.TestEntities);
        }
    }

    #endregion
}
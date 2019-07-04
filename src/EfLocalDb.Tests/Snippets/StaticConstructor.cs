using System.Collections.Generic;
using System.Threading.Tasks;
using EfLocalDb;
using Xunit;

namespace StaticConstructor
{
    #region EfStaticConstructor

    public class Tests
    {
        static SqlInstance<DbContextUsedInStatic> sqlInstance;

        static Tests()
        {
            sqlInstance = new SqlInstance<DbContextUsedInStatic>(
                builder => new DbContextUsedInStatic(builder.Options));
        }

        [Fact]
        public async Task Test()
        {
            var entity = new TheEntity
            {
                Property = "prop"
            };
            using (var database = await sqlInstance.Build(new List<object> {entity}))
            {
                Assert.Single(database.Context.TestEntities);
            }
        }
    }

    #endregion
}
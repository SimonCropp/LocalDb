using System.Collections.Generic;
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
                builder => new TheDbContext(builder.Options));
        }

        public async Task Test()
        {
            var entity = new TheEntity
            {
                Property = "prop"
            };
            using var database = await sqlInstance.Build(new List<object> {entity});
            Assert.Single(database.Context.TestEntities);
        }
    }

    #endregion
}
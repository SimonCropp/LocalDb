#if(!NETCOREAPP3_1)
using System.Collections.Generic;
using System.Threading.Tasks;
using EfLocalDb;
using Xunit;

namespace StaticConstructor
{
    #region EfClassicStaticConstructor
    public class Tests
    {
        static SqlInstance<TheDbContext> sqlInstance;

        static Tests()
        {
            sqlInstance = new(
                connection => new(connection));
        }

        public async Task Test()
        {
            TheEntity entity = new()
            {
                Property = "prop"
            };
            List<object> data = new(){entity};
            using var database = await sqlInstance.Build(data);
            Assert.Single(database.Context.TestEntities);
        }
    }
    #endregion
}
#endif
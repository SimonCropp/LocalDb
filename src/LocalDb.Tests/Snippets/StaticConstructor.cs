using System.Threading.Tasks;
using LocalDb;
using Xunit;

namespace StaticConstructor
{
    #region StaticConstructor

    public class Tests
    {
        static SqlInstance sqlInstance;

        static Tests()
        {
            sqlInstance = new SqlInstance(
                name: "StaticConstructorInstance",
                buildTemplate: TestDbBuilder.CreateTable);
        }

        [Fact]
        public async Task Test()
        {
            using (var database = await sqlInstance.Build())
            {
                await TestDbBuilder.AddData(database.Connection);
                Assert.Single(await TestDbBuilder.GetData(database.Connection));
            }
        }
    }

    #endregion
}
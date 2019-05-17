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
            var database = await sqlInstance.Build();
            using (var connection = await database.OpenConnection())
            {
                await TestDbBuilder.AddData(connection);
            }

            using (var connection = await database.OpenConnection())
            {
                Assert.Single(await TestDbBuilder.GetData(connection));
            }
        }
    }

    #endregion
}
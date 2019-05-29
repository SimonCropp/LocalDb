using System;
using System.Threading.Tasks;
using ApprovalTests;
using LocalDb;
using Xunit;
using Xunit.Abstractions;

public class Tests:
    XunitLoggingBase
{
    [Fact]
    public async Task Simple()
    {
        var instance = new SqlInstance(
            name: "Name",
            buildTemplate: TestDbBuilder.CreateTable);

        var database = await instance.Build();
        using (var connection = await database.OpenConnection())
        {
            await TestDbBuilder.AddData(connection);
        }

        using (var connection = await database.OpenConnection())
        {
            Assert.Single(await TestDbBuilder.GetData(connection));
        }
    }
    [Fact]
    public void DuplicateDbContext()
    {
        Register();
        var exception = Assert.Throws<Exception>(Register);
        Approvals.Verify(exception.Message);
    }

    static void Register()
    {
        LocalDb.SqlInstanceService.Register("LocalDbDuplicateDbContext", TestDbBuilder.CreateTable);
    }

    [Fact]
    public async Task WithRebuild()
    {
        var instance1 = new SqlInstance(
            name: "rebuild",
            buildTemplate: TestDbBuilder.CreateTable,
            requiresRebuild: dbContext => true);
        var database1 = await instance1.Build();
        using (var connection = await database1.OpenConnection())
        {
            await TestDbBuilder.AddData(connection);
        }

        var instance2 = new SqlInstance(
            name: "rebuild",
            buildTemplate: (string connection) => throw new Exception(),
            requiresRebuild: dbContext => false);
        var database2 = await instance2.Build();
        using (var connection = await database2.OpenConnection())
        {
            await TestDbBuilder.AddData(connection);
        }

        using (var connection = await database2.OpenConnection())
        {
            var data = await TestDbBuilder.GetData(connection);
            Assert.Single(data);
        }
    }

    public Tests(ITestOutputHelper output) :
        base(output)
    {
    }

}
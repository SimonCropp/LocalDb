using System;
using System.Threading.Tasks;
using ApprovalTests;
using LocalDb;
using ObjectApproval;
using Xunit;
using Xunit.Abstractions;

public class Tests :
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
        SqlInstanceService.Register("LocalDbDuplicateDbContext", TestDbBuilder.CreateTable);
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
            var data = await TestDbBuilder.GetData(connection);
            Assert.Single(data);
        }
    }

    [Fact]
    public async Task DbSettings()
    {
        var instance = new SqlInstance(
            name: "Name",
            buildTemplate: TestDbBuilder.CreateTable);

        var database = await instance.Build();
        using (var connection = await database.OpenConnection())
        {
            var settings = DbPropertyReader.Read(connection, "Tests_DbSettings");
            ObjectApprover.VerifyWithJson(settings, s => s.Replace(Path.GetTempPath(), ""));
        }
    }

    public Tests(ITestOutputHelper output) :
        base(output)
    {
    }
}
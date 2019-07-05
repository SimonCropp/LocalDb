using System;
using System.IO;
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

        using (var database = await instance.Build())
        {
            await TestDbBuilder.AddData(database.Connection);
            Assert.Single(await TestDbBuilder.GetData(database.Connection));
            var settings = DbPropertyReader.Read(database.Connection, "Tests_Simple");
            ObjectApprover.VerifyWithJson(settings, s => s.Replace(Path.GetTempPath(), ""));
        }
    }

    [Fact]
    public async Task NoFileAndNoDb()
    {
        SqlLocalDb.DeleteInstance("NoFileAndNoDb");
        var directory = DirectoryFinder.Find("NoFileAndNoDb");

        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, true);
        }
        var instance = new SqlInstance(
            name: "NoFileAndNoDb",
            buildTemplate: TestDbBuilder.CreateTable);

        using (var database = await instance.Build())
        {
            await TestDbBuilder.AddData(database.Connection);
            Assert.Single(await TestDbBuilder.GetData(database.Connection));
        }
    }

    [Fact]
    public void Duplicate()
    {
        Register();
        var exception = Assert.Throws<Exception>(Register);
        Approvals.Verify(exception.Message);
    }

    static void Register()
    {
        SqlInstanceService.Register("LocalDbDuplicate", TestDbBuilder.CreateTable);
    }

    [Fact]
    public async Task WithRebuild()
    {
        var instance1 = new SqlInstance(
            name: "rebuild",
            buildTemplate: TestDbBuilder.CreateTable,
            requiresRebuild: dbContext => true);
        using (var database1 = await instance1.Build())
        {
            await TestDbBuilder.AddData(database1.Connection);
        }

        var instance2 = new SqlInstance(
            name: "rebuild",
            buildTemplate: (string connection) => throw new Exception(),
            requiresRebuild: dbContext => false);
        using (var database2 = await instance2.Build())
        {
            await TestDbBuilder.AddData(database2.Connection);
            var data = await TestDbBuilder.GetData(database2.Connection);
            Assert.Single(data);
        }
    }

    public Tests(ITestOutputHelper output) :
        base(output)
    {
    }
}
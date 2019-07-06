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
        var instance = new SqlInstance("Name", TestDbBuilder.CreateTable);

        using (var database = await instance.Build())
        {
            var connection = database.Connection;
            var data = await TestDbBuilder.AddData(connection);
            Assert.Contains(data, await TestDbBuilder.GetData(connection));
            var settings = DbPropertyReader.Read(connection, "Tests_Simple");
            ObjectApprover.VerifyWithJson(settings, s => s.Replace(Path.GetTempPath(), ""));
        }
    }

    [Fact]
    public async Task NoFileAndNoInstance()
    {
        SqlLocalDb.DeleteInstance("NoFileAndNoInstance");
        DirectoryFinder.Delete("NoFileAndNoInstance");

        var instance = new SqlInstance("NoFileAndNoInstance", TestDbBuilder.CreateTable);

        await AddAndVerifyData(instance);
    }

    [Fact]
    public async Task WithFileAndNoInstance()
    {
        new SqlInstance("WithFileAndNoInstance", TestDbBuilder.CreateTable);
        SqlLocalDb.DeleteInstance("WithFileAndNoInstance");
        var instance = new SqlInstance("WithFileAndNoInstance", TestDbBuilder.CreateTable);

        await AddAndVerifyData(instance);
    }

    [Fact]
    public async Task NoFileAndWithInstance()
    {
        ManagedLocalDbApi.CreateInstance("NoFileAndWithInstance");
        DirectoryFinder.Delete("NoFileAndWithInstance");

        var instance = new SqlInstance("NoFileAndWithInstance", TestDbBuilder.CreateTable);

        await AddAndVerifyData(instance);
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
            "rebuild",
            TestDbBuilder.CreateTable,
            requiresRebuild: dbContext => true);
        using (var database1 = await instance1.Build())
        {
            await TestDbBuilder.AddData(database1.Connection);
        }

        var instance2 = new SqlInstance(
            "rebuild",
            (string connection) => throw new Exception(),
            requiresRebuild: dbContext => false);
        await AddAndVerifyData(instance2);
    }

    static async Task AddAndVerifyData(SqlInstance instance)
    {
        using (var database = await instance.Build())
        {
            var connection = database.Connection;
            var data = await TestDbBuilder.AddData(connection);
            Assert.Contains(data, await TestDbBuilder.GetData(connection));
        }
    }

    public Tests(ITestOutputHelper output) :
        base(output)
    {
    }
}
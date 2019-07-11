using System;
using System.Threading.Tasks;
using ApprovalTests;
using LocalDb;
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
            Assert.NotEmpty(settings.Files);
        }
    }
    [Fact]
    public async Task Multiple()
    {
        var stopwatch = Stopwatch.StartNew();
        var instance = new SqlInstance("Multiple", TestDbBuilder.CreateTable);

        using ( await instance.Build(databaseSuffix:"one"))
        {
        }
        using (await instance.Build(databaseSuffix:"two"))
        {
        }
        Trace.WriteLine(stopwatch.ElapsedMilliseconds);
    }

    [Fact]
    public async Task NoFileAndNoInstance()
    {
        LocalDbApi.StopAndDelete("NoFileAndNoInstance");
        DirectoryFinder.Delete("NoFileAndNoInstance");

        var instance = new SqlInstance("NoFileAndNoInstance", TestDbBuilder.CreateTable);

        await AddAndVerifyData(instance);
    }

    [Fact]
    public async Task WithFileAndNoInstance()
    {
        new SqlInstance("WithFileAndNoInstance", TestDbBuilder.CreateTable);
        LocalDbApi.StopAndDelete("WithFileAndNoInstance");
        var instance = new SqlInstance("WithFileAndNoInstance", TestDbBuilder.CreateTable);

        await AddAndVerifyData(instance);
    }

    [Fact]
    public async Task NoFileAndWithInstance()
    {
        LocalDbApi.CreateInstance("NoFileAndWithInstance");
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
        int data;
        using (var database1 = await instance1.Build())
        {
            data = await TestDbBuilder.AddData(database1.Connection);
        }

        var instance2 = new SqlInstance(
            "rebuild",
            connection => throw new Exception(),
            requiresRebuild: dbContext => false);
        using (var database = await instance2.Build())
        {
            var connection1 = database.Connection;
            Assert.DoesNotContain(data, await TestDbBuilder.GetData(connection1));
        }
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
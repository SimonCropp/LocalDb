using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using ApprovalTests;
using ObjectApproval;
using Xunit;
using Xunit.Abstractions;

public class WrapperTests :
    XunitLoggingBase
{
    static Wrapper instance;

    [Fact]
    public void InvalidInstanceName()
    {
        var exception = Assert.Throws<ArgumentException>(() => new Wrapper("<", "s"));
        Approvals.Verify(exception.Message);
    }

    [Fact]
    public async Task RecreateWithOpenConnection()
    {
        LocalDbApi.StopAndDelete("RecreateWithOpenConnection");
        DirectoryFinder.Delete("RecreateWithOpenConnection");

        var wrapper = new Wrapper("RecreateWithOpenConnection", DirectoryFinder.Find("RecreateWithOpenConnection"));
        wrapper.Start(timestamp, TestDbBuilder.CreateTable);
        var connectionString = await wrapper.CreateDatabaseFromTemplate("Simple");
        using (var connection = new SqlConnection(connectionString))
        {
             await connection.OpenAsync();
             await wrapper.CreateDatabaseFromTemplate("Simple");

             wrapper = new Wrapper("RecreateWithOpenConnection", DirectoryFinder.Find("RecreateWithOpenConnection"));
             wrapper.Start(timestamp, TestDbBuilder.CreateTable);
             await wrapper.CreateDatabaseFromTemplate("Simple");
        }

        ObjectApprover.VerifyWithJson(wrapper.ReadDatabaseState("Simple"));
    }
    [Fact]
    public async Task NoFileAndNoInstance()
    {
        LocalDbApi.StopAndDelete("NoFileAndNoInstance");
        DirectoryFinder.Delete("NoFileAndNoInstance");

        var wrapper = new Wrapper("NoFileAndNoInstance", DirectoryFinder.Find("NoFileAndNoInstance"));
        wrapper.Start(timestamp, TestDbBuilder.CreateTable);
        await wrapper.CreateDatabaseFromTemplate("Simple");
        ObjectApprover.VerifyWithJson(wrapper.ReadDatabaseState("Simple"));
    }

    [Fact]
    public async Task WithFileAndNoInstance()
    {
        var wrapper = new Wrapper("WithFileAndNoInstance", DirectoryFinder.Find("WithFileAndNoInstance"));
        wrapper.Start(timestamp, TestDbBuilder.CreateTable);
        await wrapper.AwaitStart();
        wrapper.DeleteInstance();
        wrapper = new Wrapper("WithFileAndNoInstance", DirectoryFinder.Find("WithFileAndNoInstance"));
        wrapper.Start(timestamp, TestDbBuilder.CreateTable);
        await wrapper.CreateDatabaseFromTemplate("Simple");
        ObjectApprover.VerifyWithJson(wrapper.ReadDatabaseState("Simple"));
    }

    //TODO: new test with a named db existing, but no file existing


    [Fact]
    public async Task NoFileAndWithInstance()
    {
        LocalDbApi.StopAndDelete("NoFileAndWithInstance");
        LocalDbApi.CreateInstance("NoFileAndWithInstance");
        DirectoryFinder.Delete("NoFileAndWithInstance");
        var wrapper = new Wrapper("NoFileAndWithInstance", DirectoryFinder.Find("NoFileAndWithInstance"));
        wrapper.Start(timestamp, TestDbBuilder.CreateTable);
        await wrapper.AwaitStart();
        await wrapper.CreateDatabaseFromTemplate("Simple");
        ObjectApprover.VerifyWithJson(wrapper.ReadDatabaseState("Simple"));
    }

    [Fact]
    public async Task DeleteDatabase()
    {
        await instance.CreateDatabaseFromTemplate("ToDelete");
        await instance.DeleteDatabase("ToDelete");
        ObjectApprover.VerifyWithJson(instance.ReadDatabaseState("ToDelete"));
    }

    [Fact]
    public Task WithRebuild()
    {
        var instance2 = new Wrapper("WrapperTests", DirectoryFinder.Find("WrapperTests"));
        instance2.Start(timestamp, connection => throw new Exception());
        return instance2.AwaitStart();
    }

    [Fact]
    public async Task CreateDatabase()
    {
        await instance.CreateDatabaseFromTemplate("CreateDatabase");
        ObjectApprover.VerifyWithJson(instance.ReadDatabaseState("CreateDatabase"));
    }

    [Fact]
    public async Task DeleteDatabaseWithOpenConnection()
    {
        var connection = await instance.CreateDatabaseFromTemplate("ToDelete");
        using (var sqlConnection = new SqlConnection(connection))
        {
            await sqlConnection.OpenAsync();
            await instance.DeleteDatabase("ToDelete");
            var deletedState = instance.ReadDatabaseState("ToDelete");
            await instance.CreateDatabaseFromTemplate("ToDelete");
            var createdState = instance.ReadDatabaseState("ToDelete");
            ObjectApprover.VerifyWithJson(new
            {
                deletedState,
                createdState
            });
        }
    }

    public WrapperTests(ITestOutputHelper output) :
        base(output)
    {
    }

    static DateTime timestamp = new DateTime(2000, 1, 1);

    static WrapperTests()
    {
        LocalDbApi.StopAndDelete("WrapperTests");
        instance = new Wrapper("WrapperTests", DirectoryFinder.Find("WrapperTests"));
        instance.Start(timestamp, TestDbBuilder.CreateTable);
        instance.AwaitStart().GetAwaiter().GetResult();
    }
}
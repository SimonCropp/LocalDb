using System;
using System.Threading.Tasks;
using VerifyXunit;
using Microsoft.Data.SqlClient;
using Xunit;
using Xunit.Abstractions;

public class WrapperTests :
    VerifyBase
{
    static Wrapper instance;

    [Fact]
    public Task InvalidInstanceName()
    {
        var exception = Assert.Throws<ArgumentException>(() => new Wrapper("<", "s"));
        return Verify(exception.Message);
    }

    [Fact(Skip = "no supported")]
    public async Task RecreateWithOpenConnectionAfterStartup()
    {
        /*
could be supported by running the following in wrapper CreateDatabaseFromTemplate
but it is fairly unlikely to happen and not doing the offline saves time in tests

if db_id('{name}') is not null
begin
    alter database [{name}] set single_user with rollback immediate;
    alter database [{name}] set multi_user;
    alter database [{name}] set offline;
end;
         */
        LocalDbApi.StopAndDelete("RecreateWithOpenConnectionAfterStartup");
        DirectoryFinder.Delete("RecreateWithOpenConnectionAfterStartup");

        var wrapper = new Wrapper("RecreateWithOpenConnectionAfterStartup", DirectoryFinder.Find("RecreateWithOpenConnectionAfterStartup"));
        wrapper.Start(timestamp, TestDbBuilder.CreateTable);
        var connectionString = await wrapper.CreateDatabaseFromTemplate("Simple");
        await using (var connection = new SqlConnection(connectionString))
        {
             await connection.OpenAsync();
             await wrapper.CreateDatabaseFromTemplate("Simple");

             wrapper = new Wrapper("RecreateWithOpenConnectionAfterStartup", DirectoryFinder.Find("RecreateWithOpenConnection"));
             wrapper.Start(timestamp, TestDbBuilder.CreateTable);
             await wrapper.CreateDatabaseFromTemplate("Simple");
        }

        await Verify(await wrapper.ReadDatabaseState("Simple"));
        LocalDbApi.StopInstance("RecreateWithOpenConnectionAfterStartup");
    }

    [Fact]
    public async Task RecreateWithOpenConnection()
    {
        LocalDbApi.StopAndDelete("RecreateWithOpenConnection");
        DirectoryFinder.Delete("RecreateWithOpenConnection");

        var wrapper = new Wrapper("RecreateWithOpenConnection", DirectoryFinder.Find("RecreateWithOpenConnection"));
        wrapper.Start(timestamp, TestDbBuilder.CreateTable);
        var connectionString = await wrapper.CreateDatabaseFromTemplate("Simple");
        await using (var connection = new SqlConnection(connectionString))
        {
             await connection.OpenAsync();
             wrapper = new Wrapper("RecreateWithOpenConnection", DirectoryFinder.Find("RecreateWithOpenConnection"));
             wrapper.Start(timestamp, TestDbBuilder.CreateTable);
             await wrapper.CreateDatabaseFromTemplate("Simple");
        }

        await Verify(await wrapper.ReadDatabaseState("Simple"));
        LocalDbApi.StopInstance("RecreateWithOpenConnection");
    }

    [Fact]
    public async Task NoFileAndNoInstance()
    {
        LocalDbApi.StopAndDelete("NoFileAndNoInstance");
        DirectoryFinder.Delete("NoFileAndNoInstance");

        var wrapper = new Wrapper("NoFileAndNoInstance", DirectoryFinder.Find("NoFileAndNoInstance"));
        wrapper.Start(timestamp, TestDbBuilder.CreateTable);
        await wrapper.CreateDatabaseFromTemplate("Simple");
        await Verify(await wrapper.ReadDatabaseState("Simple"));
        LocalDbApi.StopInstance("NoFileAndNoInstance");
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
        await Verify(await wrapper.ReadDatabaseState("Simple"));
        LocalDbApi.StopInstance("WithFileAndNoInstance");
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
        await Verify(await wrapper.ReadDatabaseState("Simple"));
        LocalDbApi.StopInstance("NoFileAndWithInstance");
    }

    [Fact]
    public async Task DeleteDatabase()
    {
        await instance.CreateDatabaseFromTemplate("ToDelete");
        await instance.DeleteDatabase("ToDelete");
        await Verify(await instance.ReadDatabaseState("ToDelete"));
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
        await Verify(await instance.ReadDatabaseState("CreateDatabase"));
    }

    [Fact]
    public async Task DeleteDatabaseWithOpenConnection()
    {
        var connection = await instance.CreateDatabaseFromTemplate("ToDelete");
        await using var sqlConnection = new SqlConnection(connection);
        await sqlConnection.OpenAsync();
        await instance.DeleteDatabase("ToDelete");
        var deletedState = await instance.ReadDatabaseState("ToDelete");
        await instance.CreateDatabaseFromTemplate("ToDelete");
        var createdState = await instance.ReadDatabaseState("ToDelete");
        await Verify(new
        {
            deletedState,
            createdState
        });
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
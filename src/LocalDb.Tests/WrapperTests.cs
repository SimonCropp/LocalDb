[Collection("Sequential")]
public class WrapperTests
{
    static Wrapper instance;

    [Fact]
    public Task InvalidInstanceName()
    {
        var exception = Assert.Throws<ArgumentException>(() => new Wrapper(s => new SqlConnection(s), "<", "s"));
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
        var name = "RecreateWithOpenConnectionAfterStartup";
        LocalDbApi.StopAndDelete(name);
        DirectoryFinder.Delete(name);

        var wrapper = new Wrapper(s => new SqlConnection(s), name, DirectoryFinder.Find(name));
        wrapper.Start(timestamp, TestDbBuilder.CreateTable);
        var connectionString = await wrapper.CreateDatabaseFromTemplate("Simple");
        await using (var connection = new SqlConnection(connectionString))
        {
            await connection.OpenAsync();
            await wrapper.CreateDatabaseFromTemplate("Simple");

            wrapper = new(s => new SqlConnection(s), name, DirectoryFinder.Find("RecreateWithOpenConnection"));
            wrapper.Start(timestamp, TestDbBuilder.CreateTable);
            await wrapper.CreateDatabaseFromTemplate("Simple");
        }

        await Verify(wrapper.ReadDatabaseState("Simple"));
        LocalDbApi.StopInstance(name);
    }

    [Fact]
    public async Task RecreateWithOpenConnection()
    {
        var name = "RecreateWithOpenConnection";
        LocalDbApi.StopAndDelete(name);
        DirectoryFinder.Delete(name);

        var wrapper = new Wrapper(s => new SqlConnection(s), name, DirectoryFinder.Find(name));
        wrapper.Start(timestamp, TestDbBuilder.CreateTable);
        var connectionString = await wrapper.CreateDatabaseFromTemplate("Simple");
        await using (var connection = new SqlConnection(connectionString))
        {
            await connection.OpenAsync();
            wrapper = new(s => new SqlConnection(s), name, DirectoryFinder.Find(name));
            wrapper.Start(timestamp, TestDbBuilder.CreateTable);
            await wrapper.CreateDatabaseFromTemplate("Simple");
        }

        await Verify(wrapper.ReadDatabaseState("Simple"));
        LocalDbApi.StopInstance(name);
    }

    [Fact]
    public async Task NoFileAndNoInstance()
    {
        var name = "NoFileAndNoInstance";
        LocalDbApi.StopAndDelete(name);
        DirectoryFinder.Delete(name);

        var wrapper = new Wrapper(s => new SqlConnection(s), name, DirectoryFinder.Find(name));
        wrapper.Start(timestamp, TestDbBuilder.CreateTable);
        await wrapper.CreateDatabaseFromTemplate("Simple");
        await Verify(wrapper.ReadDatabaseState("Simple"));
        LocalDbApi.StopInstance(name);
    }

    [Fact]
    public async Task Callback()
    {
        var name = "WrapperTests_Callback";

        var callbackCalled = false;
        var wrapper = new Wrapper(s => new SqlConnection(s), name, DirectoryFinder.Find(name), callback: _ =>
        {
            callbackCalled = true;
            return Task.CompletedTask;
        });
        wrapper.Start(timestamp, TestDbBuilder.CreateTable);
        await wrapper.CreateDatabaseFromTemplate("Simple");
        Assert.True(callbackCalled);
        LocalDbApi.StopAndDelete(name);
    }

    [Fact]
    public async Task WithFileAndNoInstance()
    {
        var name = "WithFileAndNoInstance";
        var wrapper = new Wrapper(s => new SqlConnection(s), name, DirectoryFinder.Find(name));
        wrapper.Start(timestamp, TestDbBuilder.CreateTable);
        await wrapper.AwaitStart();
        wrapper.DeleteInstance();
        wrapper = new(s => new SqlConnection(s), name, DirectoryFinder.Find(name));
        wrapper.Start(timestamp, TestDbBuilder.CreateTable);
        await wrapper.CreateDatabaseFromTemplate("Simple");
        await Verify(wrapper.ReadDatabaseState("Simple"));
        LocalDbApi.StopInstance(name);
    }

    [Fact]
    public async Task NoFileAndWithInstanceAndNamedDb()
    {
        var instanceName = "NoFileAndWithInstanceAndNamedDb";
        LocalDbApi.StopAndDelete(instanceName);
        LocalDbApi.CreateInstance(instanceName);
        DirectoryFinder.Delete(instanceName);
        var wrapper = new Wrapper(s => new SqlConnection(s), instanceName, DirectoryFinder.Find(instanceName));
        wrapper.Start(timestamp, TestDbBuilder.CreateTable);
        await wrapper.AwaitStart();
        await wrapper.CreateDatabaseFromTemplate("Simple");

        Thread.Sleep(3000);
        DirectoryFinder.Delete(instanceName);

        wrapper = new(s => new SqlConnection(s), instanceName, DirectoryFinder.Find(instanceName));
        wrapper.Start(timestamp, TestDbBuilder.CreateTable);
        await wrapper.AwaitStart();
        await wrapper.CreateDatabaseFromTemplate("Simple");

        await Verify(wrapper.ReadDatabaseState("Simple"));
    }

    [Fact]
    public async Task NoFileAndWithInstance()
    {
        var name = "NoFileAndWithInstance";
        LocalDbApi.StopAndDelete(name);
        LocalDbApi.CreateInstance(name);
        DirectoryFinder.Delete(name);
        var wrapper = new Wrapper(s => new SqlConnection(s), name, DirectoryFinder.Find(name));
        wrapper.Start(timestamp, TestDbBuilder.CreateTable);
        await wrapper.AwaitStart();
        await wrapper.CreateDatabaseFromTemplate("Simple");
        await Verify(wrapper.ReadDatabaseState("Simple"));
        LocalDbApi.StopInstance(name);
    }

    [Fact]
    public async Task DeleteDatabase()
    {
        await instance.CreateDatabaseFromTemplate("ToDelete");
        Recording.Start();
        await instance.DeleteDatabase("ToDelete");
        await Verify();
    }

    [Fact]
    public async Task DefinedTimestamp()
    {
        var name = "DefinedTimestamp";
        var instance2 = new Wrapper(s => new SqlConnection(s), name, DirectoryFinder.Find(name));
        var dateTime = DateTime.Now;
        instance2.Start(dateTime, _ => Task.CompletedTask);
        await instance2.AwaitStart();
        Assert.Equal(dateTime, File.GetCreationTime(instance2.DataFile));
    }

    [Fact]
    public async Task WithRebuild()
    {
        var instance2 = new Wrapper(s => new SqlConnection(s), "WrapperTests", DirectoryFinder.Find("WrapperTests"));

        Recording.Start();
        instance2.Start(timestamp, _ => throw new());
        await instance2.AwaitStart();
        await Verify();
    }

    [Fact]
    public async Task CreateDatabase()
    {
        Recording.Start();
        await instance.CreateDatabaseFromTemplate("CreateDatabase");
        var entries = Recording.Stop();
        await Verify(
            new
            {
                entries,
                state = await instance.ReadDatabaseState("CreateDatabase")
            });
    }

    [Fact]
    public async Task DeleteDatabaseWithOpenConnection()
    {
        var name = "ToDelete";
        var connectionString = await instance.CreateDatabaseFromTemplate(name);
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await instance.DeleteDatabase(name);
        var deletedState = await instance.ReadDatabaseState(name);

        Recording.Start();
        await instance.CreateDatabaseFromTemplate(name);
        var entries = Recording.Stop();

        var createdState = await instance.ReadDatabaseState(name);
        await Verify(new
        {
            entries,
            deletedState,
            createdState
        });
    }

    static DateTime timestamp = new(2000, 1, 1);

    static WrapperTests()
    {
        LocalDbApi.StopAndDelete("WrapperTests");
        instance = new(_ => new SqlConnection(_), "WrapperTests", DirectoryFinder.Find("WrapperTests"));
        instance.Start(timestamp, TestDbBuilder.CreateTable);
        instance.AwaitStart().GetAwaiter().GetResult();
    }
}
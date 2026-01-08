[TestFixture]
public class WrapperTests
{
    static Wrapper instance;

    [Test]
    public Task InvalidInstanceName()
    {
        var exception = Throws<ArgumentException>(() => new Wrapper(_ => new SqlConnection(_), "<", "s"))!;
        return Verify(exception.Message);
    }

    [Test]
    public void InvalidDatabaseName_InvalidCharAtStart()
    {
        // Test that invalid characters at position 0 are caught (bug fix test)
        var exception = ThrowsAsync<ArgumentException>(
            async () => await instance.CreateDatabaseFromTemplate("<InvalidName"));
        NotNull(exception);
    }

    [Test]
    public void InvalidDatabaseName_InvalidCharInMiddle()
    {
        // Test that invalid characters in the middle are also caught
        var exception = ThrowsAsync<ArgumentException>(
            async () => await instance.CreateDatabaseFromTemplate("Invalid<Name"));
        NotNull(exception);
    }

    [Test]
    [Explicit]
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

        using var wrapper = new Wrapper(_ => new SqlConnection(_), name, DirectoryFinder.Find(name));
        wrapper.Start(timestamp, TestDbBuilder.CreateTable);
        var connectionString = await wrapper.CreateDatabaseFromTemplate("Simple");
        await using (var connection = new SqlConnection(connectionString))
        {
            await connection.OpenAsync();
            await wrapper.CreateDatabaseFromTemplate("Simple");

            using var innerWrapper = new Wrapper(_ => new SqlConnection(_), name, DirectoryFinder.Find("RecreateWithOpenConnection"));
            innerWrapper.Start(timestamp, TestDbBuilder.CreateTable);
            await innerWrapper.CreateDatabaseFromTemplate("Simple");
        }

        await Verify(wrapper.ReadDatabaseState("Simple"));
        LocalDbApi.StopInstance(name);
    }

    [Test]
    public async Task RecreateWithOpenConnection()
    {
        var name = "RecreateWithOpenConnection";
        LocalDbApi.StopAndDelete(name);
        DirectoryFinder.Delete(name);

        using var wrapper = new Wrapper(_ => new SqlConnection(_), name, DirectoryFinder.Find(name));
        wrapper.Start(timestamp, TestDbBuilder.CreateTable);
        var connectionString = await wrapper.CreateDatabaseFromTemplate("Simple");
        await using (var connection = new SqlConnection(connectionString))
        {
            await connection.OpenAsync();
            using var innerWrapper = new Wrapper(_ => new SqlConnection(_), name, DirectoryFinder.Find(name));
            innerWrapper.Start(timestamp, TestDbBuilder.CreateTable);
            await innerWrapper.CreateDatabaseFromTemplate("Simple");
        }

        await Verify(wrapper.ReadDatabaseState("Simple"));
        LocalDbApi.StopInstance(name);
    }

    [Test]
    public async Task NoFileAndNoInstance()
    {
        var name = "NoFileAndNoInstance";
        LocalDbApi.StopAndDelete(name);
        DirectoryFinder.Delete(name);

        using var wrapper = new Wrapper(_ => new SqlConnection(_), name, DirectoryFinder.Find(name));
        wrapper.Start(timestamp, TestDbBuilder.CreateTable);
        await wrapper.CreateDatabaseFromTemplate("Simple");
        await Verify(wrapper.ReadDatabaseState("Simple"));
        LocalDbApi.StopInstance(name);
    }

    [Test]
    public async Task Callback()
    {
        var name = "WrapperTests_Callback";

        var callbackCalled = false;
        using var wrapper = new Wrapper(_ => new SqlConnection(_), name, DirectoryFinder.Find(name), callback: _ =>
        {
            callbackCalled = true;
            return Task.CompletedTask;
        });
        wrapper.Start(timestamp, TestDbBuilder.CreateTable);
        await wrapper.CreateDatabaseFromTemplate("Simple");
        True(callbackCalled);
        LocalDbApi.StopAndDelete(name);
    }

    [Test]
    public async Task WithFileAndNoInstance()
    {
        var name = "WithFileAndNoInstance";
        var wrapper = new Wrapper(_ => new SqlConnection(_), name, DirectoryFinder.Find(name));
        wrapper.Start(timestamp, TestDbBuilder.CreateTable);
        await wrapper.AwaitStart();
        wrapper.DeleteInstance();
        wrapper.Dispose();
        wrapper = new(_ => new SqlConnection(_), name, DirectoryFinder.Find(name));
        wrapper.Start(timestamp, TestDbBuilder.CreateTable);
        await wrapper.CreateDatabaseFromTemplate("Simple");
        await Verify(wrapper.ReadDatabaseState("Simple"));
        wrapper.Dispose();
        LocalDbApi.StopInstance(name);
    }

    [Test]
    public async Task NoFileAndWithInstanceAndNamedDb()
    {
        var instanceName = "NoFileAndWithInstanceAndNamedDb";
        LocalDbApi.StopAndDelete(instanceName);
        LocalDbApi.CreateInstance(instanceName);
        DirectoryFinder.Delete(instanceName);
        using var wrapper = new Wrapper(_ => new SqlConnection(_), instanceName, DirectoryFinder.Find(instanceName));
        wrapper.Start(timestamp, TestDbBuilder.CreateTable);
        await wrapper.AwaitStart();
        await wrapper.CreateDatabaseFromTemplate("Simple");

        Thread.Sleep(3000);
        DirectoryFinder.Delete(instanceName);

        using var newWrapper = new Wrapper(_ => new SqlConnection(_), instanceName, DirectoryFinder.Find(instanceName));
        newWrapper.Start(timestamp, TestDbBuilder.CreateTable);
        await newWrapper.AwaitStart();
        await newWrapper.CreateDatabaseFromTemplate("Simple");

        await Verify(newWrapper.ReadDatabaseState("Simple"));
    }

    [Test]
    public async Task NoFileAndWithInstance()
    {
        var name = "NoFileAndWithInstance";
        LocalDbApi.StopAndDelete(name);
        LocalDbApi.CreateInstance(name);
        DirectoryFinder.Delete(name);
        using var wrapper = new Wrapper(_ => new SqlConnection(_), name, DirectoryFinder.Find(name));
        wrapper.Start(timestamp, TestDbBuilder.CreateTable);
        await wrapper.AwaitStart();
        await wrapper.CreateDatabaseFromTemplate("Simple");
        await Verify(wrapper.ReadDatabaseState("Simple"));
        LocalDbApi.StopInstance(name);
    }

    [Test]
    public async Task DeleteDatabase()
    {
        await instance.CreateDatabaseFromTemplate("ToDelete");
        Recording.Start();
        await instance.DeleteDatabase("ToDelete");
        await Verify();
    }

    [Test]
    public async Task UnicodeDatabaseNameSupport()
    {
        // Test Japanese database name
        // Creates DB, then recreates WITHOUT deleting to verify db_id(N'{name}') recognizes Unicode DB
        var japaneseName = "テスト用データベース";
        await instance.CreateDatabaseFromTemplate(japaneseName);
        await instance.CreateDatabaseFromTemplate(japaneseName); // Recreate without deleting - tests OFFLINE logic
        var japaneseState = await instance.ReadDatabaseState(japaneseName);
        True(japaneseState.DataFileExists);
        True(japaneseState.LogFileExists);
        await instance.DeleteDatabase(japaneseName);

        // Test Chinese database name
        var chineseName = "测试数据库";
        await instance.CreateDatabaseFromTemplate(chineseName);
        await instance.CreateDatabaseFromTemplate(chineseName); // Recreate without deleting
        var chineseState = await instance.ReadDatabaseState(chineseName);
        True(chineseState.DataFileExists);
        True(chineseState.LogFileExists);
        await instance.DeleteDatabase(chineseName);

        // Test Korean database name
        var koreanName = "테스트데이터베이스";
        await instance.CreateDatabaseFromTemplate(koreanName);
        await instance.CreateDatabaseFromTemplate(koreanName); // Recreate without deleting
        var koreanState = await instance.ReadDatabaseState(koreanName);
        True(koreanState.DataFileExists);
        True(koreanState.LogFileExists);
        await instance.DeleteDatabase(koreanName);

        // Test delete and recreate (different code path)
        await instance.CreateDatabaseFromTemplate(japaneseName);
        await instance.DeleteDatabase(japaneseName);
        await instance.CreateDatabaseFromTemplate(japaneseName); // Create after delete
        var recreatedState = await instance.ReadDatabaseState(japaneseName);
        True(recreatedState.DataFileExists);
        await instance.DeleteDatabase(japaneseName);
    }

    [Test]
    public async Task DefinedTimestamp()
    {
        var name = "DefinedTimestamp";
        using var wrapper = new Wrapper(_ => new SqlConnection(_), name, DirectoryFinder.Find(name));
        var dateTime = DateTime.Now;
        wrapper.Start(dateTime, _ => Task.CompletedTask);
        await wrapper.AwaitStart();
        AreEqual(dateTime, File.GetCreationTime(wrapper.DataFile));
    }

    [Test]
    public async Task WithRebuild()
    {
        using var wrapper = new Wrapper(
            _ => new SqlConnection(_),
            "WrapperTests",
            DirectoryFinder.Find("WrapperTests"));

        Recording.Start();
        wrapper.Start(timestamp, _ => throw new());
        await wrapper.AwaitStart();
        await Verify();
    }

    [Test]
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

    [Test]
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

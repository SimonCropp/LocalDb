[TestFixture]
public class WrapperTests
{
    static Wrapper instance;

    [Test]
    public Task InvalidInstanceName()
    {
        var exception = Throws<ArgumentException>(() => new Wrapper("<", "s"))!;
        return Verify(exception.Message);
    }

    [Test]
    public void InvalidDatabaseName_InvalidCharAtStart()
    {
        // Test that invalid characters at position 0 are caught (bug fix test)
        var exception = ThrowsAsync<ArgumentException>(async () => await instance.CreateDatabaseFromTemplate("<InvalidName"));
        NotNull(exception);
    }

    [Test]
    public void InvalidDatabaseName_InvalidCharInMiddle()
    {
        // Test that invalid characters in the middle are also caught
        var exception = ThrowsAsync<ArgumentException>(async () => await instance.CreateDatabaseFromTemplate("Invalid<Name"));
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

        using var wrapper = new Wrapper(name, DirectoryFinder.Find(name));
        wrapper.Start(timestamp, TestDbBuilder.CreateTable);
        await using (await wrapper.CreateDatabaseFromTemplate("Simple"))
        {
            await wrapper.CreateDatabaseFromTemplate("Simple");

            using var innerWrapper = new Wrapper(name, DirectoryFinder.Find("RecreateWithOpenConnection"));
            innerWrapper.Start(timestamp, TestDbBuilder.CreateTable);
            await using (await innerWrapper.CreateDatabaseFromTemplate("Simple"))
            {
            }
        }

        await Verify(wrapper.ReadDatabaseState("Simple"));
        wrapper.DeleteInstance();
    }

    [Test]
    public async Task RecreateWithOpenConnection()
    {
        var name = "RecreateWithOpenConnection";
        LocalDbApi.StopAndDelete(name);
        DirectoryFinder.Delete(name);

        using var wrapper = new Wrapper(name, DirectoryFinder.Find(name));
        wrapper.Start(timestamp, TestDbBuilder.CreateTable);
        await using (await wrapper.CreateDatabaseFromTemplate("Simple"))
        {
            using var innerWrapper = new Wrapper(name, DirectoryFinder.Find(name));
            innerWrapper.Start(timestamp, TestDbBuilder.CreateTable);
            await using (await innerWrapper.CreateDatabaseFromTemplate("Simple"))
            {
            }
        }

        await Verify(wrapper.ReadDatabaseState("Simple"));
        wrapper.DeleteInstance();
    }

    [Test]
    public async Task NoFileAndNoInstance()
    {
        var name = "NoFileAndNoInstance";
        LocalDbApi.StopAndDelete(name);
        DirectoryFinder.Delete(name);

        using var wrapper = new Wrapper(name, DirectoryFinder.Find(name));
        wrapper.Start(timestamp, TestDbBuilder.CreateTable);
        await wrapper.CreateDatabaseFromTemplate("Simple");
        await Verify(wrapper.ReadDatabaseState("Simple"));
        wrapper.DeleteInstance();
    }

    [Test]
    public async Task Callback()
    {
        var name = "WrapperTests_Callback";

        var callbackCalled = false;
        using var wrapper = new Wrapper(
            name,
            DirectoryFinder.Find(name),
            callback: _ =>
            {
                callbackCalled = true;
                return Task.CompletedTask;
            });
        wrapper.Start(timestamp, TestDbBuilder.CreateTable);
        await wrapper.CreateDatabaseFromTemplate("Simple");
        True(callbackCalled);
        wrapper.DeleteInstance();
    }

    [Test]
    public async Task CallbackReceivesOpenConnection()
    {
        var name = "WrapperTests_CallbackReceivesOpenConnection";

        ConnectionState? connectionState = null;
        var queryResult = -1;

        using var wrapper = new Wrapper(
            name,
            DirectoryFinder.Find(name),
            callback: async connection =>
            {
                connectionState = connection.State;

                // Verify we can actually execute a query on the open connection
                await using var command = connection.CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM MyTable";
                queryResult = (int)(await command.ExecuteScalarAsync())!;
            });
        wrapper.Start(timestamp, TestDbBuilder.CreateTable);
        await wrapper.CreateDatabaseFromTemplate("Simple");

        AreEqual(ConnectionState.Open, connectionState);
        AreEqual(0, queryResult); // Table exists and is empty
        wrapper.DeleteInstance();
    }

    [Test]
    public async Task CallbackInvokedWhenTemplateExists()
    {
        var name = "WrapperTests_CallbackInvokedWhenTemplateExists";
        LocalDbApi.StopAndDelete(name);
        DirectoryFinder.Delete(name);

        var callCount = 0;

        using var wrapper = new Wrapper(
            name,
            DirectoryFinder.Find(name),
            callback: async connection =>
            {
                callCount++;
                // Verify connection works
                await using var command = connection.CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM MyTable";
                await command.ExecuteScalarAsync();
            });

        // First start - template is created and callback should be invoked
        wrapper.Start(timestamp, TestDbBuilder.CreateTable);
        await wrapper.AwaitStart();
        AreEqual(1, callCount);

        // Create a database to ensure template exists
        await using (await wrapper.CreateDatabaseFromTemplate("Test1"))
        {
        }

        wrapper.DeleteInstance();
    }

    [Test]
    public async Task CallbackInvokedOnRebuild()
    {
        var name = "WrapperTests_CallbackInvokedOnRebuild";
        LocalDbApi.StopAndDelete(name);
        DirectoryFinder.Delete(name);

        var rebuildCallCount = 0;
        var nonRebuildCallCount = 0;

        using var wrapper = new Wrapper(
            name,
            DirectoryFinder.Find(name),
            callback: async connection =>
            {
                rebuildCallCount++;
                await using var command = connection.CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM MyTable";
                await command.ExecuteScalarAsync();
            });

        // First start - template is created (rebuild scenario)
        wrapper.Start(timestamp, TestDbBuilder.CreateTable);
        await wrapper.AwaitStart();
        AreEqual(1, rebuildCallCount);

        wrapper.DeleteInstance();
        wrapper.Dispose();

        // Second wrapper with existing template files (non-rebuild scenario)
        using var wrapper2 = new Wrapper(
            name,
            DirectoryFinder.Find(name),
            callback: async connection =>
            {
                nonRebuildCallCount++;
                await using var command = connection.CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM MyTable";
                await command.ExecuteScalarAsync();
            });

        wrapper2.Start(timestamp, TestDbBuilder.CreateTable);
        await wrapper2.AwaitStart();
        AreEqual(1, nonRebuildCallCount);

        wrapper2.DeleteInstance();
    }

    [Test]
    public async Task CallbackCanModifyTemplateDatabase()
    {
        var name = "WrapperTests_CallbackCanModifyTemplate";
        LocalDbApi.StopAndDelete(name);
        DirectoryFinder.Delete(name);

        using var wrapper = new Wrapper(
            name,
            DirectoryFinder.Find(name),
            callback: async connection =>
            {
                // Insert test data into template
                await using var command = connection.CreateCommand();
                command.CommandText = "INSERT INTO MyTable (Value) VALUES (42), (100)";
                await command.ExecuteNonQueryAsync();
            });

        wrapper.Start(timestamp, TestDbBuilder.CreateTable);
        await wrapper.AwaitStart();

        // Create database from template and verify callback modifications are present
        await using var database = await wrapper.CreateDatabaseFromTemplate("Test");
        await using var queryCommand = database.CreateCommand();
        queryCommand.CommandText = "SELECT COUNT(*) FROM MyTable";
        var count = (int)(await queryCommand.ExecuteScalarAsync())!;
        AreEqual(2, count);

        wrapper.DeleteInstance();
    }

    [Test]
    public async Task TemplateDetachedAfterCallback()
    {
        var name = "WrapperTests_TemplateDetachedAfterCallback";
        LocalDbApi.StopAndDelete(name);
        DirectoryFinder.Delete(name);

        using var wrapper = new Wrapper(
            name,
            DirectoryFinder.Find(name),
            callback: async connection =>
            {
                await using var command = connection.CreateCommand();
                command.CommandText = "SELECT 1";
                await command.ExecuteScalarAsync();
            });

        wrapper.Start(timestamp, TestDbBuilder.CreateTable);
        await wrapper.AwaitStart();

        // Verify template database is not attached after startup
        await using var masterConnection = new SqlConnection(wrapper.MasterConnectionString);
        await masterConnection.OpenAsync();
        await using var checkCommand = masterConnection.CreateCommand();
        checkCommand.CommandText = "SELECT COUNT(*) FROM sys.databases WHERE name = 'template'";
        var templateExists = (int)(await checkCommand.ExecuteScalarAsync())!;
        AreEqual(0, templateExists);

        wrapper.DeleteInstance();
    }

    [Test]
    public async Task WithFileAndNoInstance()
    {
        var name = "WithFileAndNoInstance";
        var wrapper = new Wrapper(name, DirectoryFinder.Find(name));
        wrapper.Start(timestamp, TestDbBuilder.CreateTable);
        await wrapper.AwaitStart();
        wrapper.DeleteInstance();
        wrapper.Dispose();
        wrapper = new(name, DirectoryFinder.Find(name));
        wrapper.Start(timestamp, TestDbBuilder.CreateTable);
        await wrapper.CreateDatabaseFromTemplate("Simple");
        await Verify(wrapper.ReadDatabaseState("Simple"));
        wrapper.Dispose();
        wrapper.DeleteInstance();
    }

    [Test]
    public async Task NoFileAndWithInstanceAndNamedDb()
    {
        var instanceName = "NoFileAndWithInstanceAndNamedDb";
        LocalDbApi.StopAndDelete(instanceName);
        LocalDbApi.CreateInstance(instanceName);
        DirectoryFinder.Delete(instanceName);
        var wrapper = new Wrapper(instanceName, DirectoryFinder.Find(instanceName));
        wrapper.Start(timestamp, TestDbBuilder.CreateTable);
        await wrapper.AwaitStart();
        await wrapper.CreateDatabaseFromTemplate("Simple");

        Thread.Sleep(3000);
        wrapper.Dispose();
        LocalDbApi.StopAndDelete(instanceName);
        DirectoryFinder.Delete(instanceName);

        using var newWrapper = new Wrapper(instanceName, DirectoryFinder.Find(instanceName));
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
        using var wrapper = new Wrapper(name, DirectoryFinder.Find(name));
        wrapper.Start(timestamp, TestDbBuilder.CreateTable);
        await wrapper.AwaitStart();
        await wrapper.CreateDatabaseFromTemplate("Simple");
        await Verify(wrapper.ReadDatabaseState("Simple"));
        wrapper.DeleteInstance();
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
        using var wrapper = new Wrapper(name, DirectoryFinder.Find(name));
        var dateTime = DateTime.Now;
        wrapper.Start(dateTime, _ => Task.CompletedTask);
        await wrapper.AwaitStart();
        AreEqual(dateTime, File.GetCreationTime(wrapper.DataFile));
        wrapper.DeleteInstance();
    }

    [Test]
    public async Task WithRebuild()
    {
        using var wrapper = new Wrapper(
            "WrapperTests",
            DirectoryFinder.Find("WrapperTests"));

        Recording.Start();
        wrapper.Start(timestamp, _ => throw new());
        await wrapper.AwaitStart();
        await Verify();
        wrapper.DeleteInstance();
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
        await using var connection = await instance.CreateDatabaseFromTemplate(name);
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
        instance = new("WrapperTests", DirectoryFinder.Find("WrapperTests"));
        instance.Start(timestamp, TestDbBuilder.CreateTable);
        instance.AwaitStart().GetAwaiter().GetResult();
    }

    [Test]
    public async Task StoppedInstanceReconstitution_UsesExistingTemplate()
    {
        // This test verifies that when an instance is stopped (but files exist),
        // we start it and reuse the existing template instead of deleting and recreating.
        var name = "StoppedInstanceReconstitution";
        LocalDbApi.StopAndDelete(name);
        DirectoryFinder.Delete(name);

        var buildTemplateCallCount = 0;

        // First: Create instance with template
        using (var wrapper = new Wrapper(name, DirectoryFinder.Find(name)))
        {
            wrapper.Start(timestamp, connection =>
            {
                buildTemplateCallCount++;
                return TestDbBuilder.CreateTable(connection);
            });
            await wrapper.AwaitStart();

            // Verify template was built
            AreEqual(1, buildTemplateCallCount);

            // Create a database to ensure everything works
            await using (await wrapper.CreateDatabaseFromTemplate("TestDb1"))
            {
            }
        }

        // Stop the instance (but keep files on disk)
        LocalDbApi.StopInstance(name, ShutdownMode.KillProcess);

        // Verify instance exists but is stopped
        var info = LocalDbApi.GetInstance(name);
        True(info.Exists, "Instance should exist");
        False(info.IsRunning, "Instance should be stopped");

        // Verify template files still exist
        var directory = DirectoryFinder.Find(name);
        True(File.Exists(Path.Combine(directory, "template.mdf")), "Template data file should exist");
        True(File.Exists(Path.Combine(directory, "template_log.ldf")), "Template log file should exist");

        // Second: Create new wrapper with same timestamp - should NOT rebuild template
        buildTemplateCallCount = 0;
        using (var wrapper = new Wrapper(name, DirectoryFinder.Find(name)))
        {
            wrapper.Start(timestamp, connection =>
            {
                buildTemplateCallCount++;
                return TestDbBuilder.CreateTable(connection);
            });
            await wrapper.AwaitStart();

            // buildTemplate should NOT have been called because:
            // 1. Instance was started (not deleted/recreated)
            // 2. Template file exists
            // 3. Timestamp matches
            AreEqual(0, buildTemplateCallCount, "buildTemplate should not be called when reconstituting stopped instance with matching timestamp");

            // Verify we can create databases from the existing template
            await using var connection = await wrapper.CreateDatabaseFromTemplate("TestDb2");
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM MyTable";
            var result = await command.ExecuteScalarAsync();
            AreEqual(0, result); // Table exists and is empty

            wrapper.DeleteInstance();
        }
    }

    [Test]
    public async Task StoppedInstanceReconstitution_RebuildWhenTimestampDiffers()
    {
        // This test verifies that when an instance is stopped with existing template,
        // but timestamp differs, we rebuild the template (not cold start).
        var name = "StoppedInstanceRebuild";
        LocalDbApi.StopAndDelete(name);
        DirectoryFinder.Delete(name);


        // First: Create instance with template using original timestamp
        using (var wrapper = new Wrapper(name, DirectoryFinder.Find(name)))
        {
            var buildTemplateCallCount = 0;
            wrapper.Start(timestamp, connection =>
            {
                buildTemplateCallCount++;
                return TestDbBuilder.CreateTable(connection);
            });
            await wrapper.AwaitStart();
            AreEqual(1, buildTemplateCallCount);
        }

        // Stop the instance
        LocalDbApi.StopInstance(name, ShutdownMode.KillProcess);

        // Second: Create new wrapper with DIFFERENT timestamp - should rebuild template
        var newTimestamp = new DateTime(2001, 1, 1);
        using (var wrapper = new Wrapper(name, DirectoryFinder.Find(name)))
        {
            var buildTemplateCallCount = 0;
            wrapper.Start(newTimestamp, connection =>
            {
                buildTemplateCallCount++;
                return TestDbBuilder.CreateTable(connection);
            });
            await wrapper.AwaitStart();

            // buildTemplate SHOULD be called because timestamp differs
            AreEqual(1, buildTemplateCallCount, "buildTemplate should be called when timestamp differs");

            wrapper.DeleteInstance();
        }
    }

    [Test]
    public async Task SharedDatabaseFileSizeIsStable()
    {
        var name = "SharedDatabaseFileSize";
        LocalDbApi.StopAndDelete(name);
        DirectoryFinder.Delete(name);

        using var wrapper = new Wrapper(name, DirectoryFinder.Find(name));
        wrapper.Start(timestamp, async connection =>
        {
            await using var command = connection.CreateCommand();
            command.CommandText =
                """
                create table MyTable (Value int);
                insert into MyTable (Value)
                select top 1000 row_number() over (order by (select null))
                from sys.all_objects;
                """;
            await command.ExecuteNonQueryAsync();
        });

        await using var connection = await wrapper.OpenSharedDatabase();
        var size = wrapper.GetSharedFileSize();

        // Read from the shared database (simulating read-only usage)
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM MyTable";
        await command.ExecuteScalarAsync();

        // Verify ThrowIfSharedDatabaseModified does not throw
        await wrapper.ThrowIfSharedDatabaseModified(size);

        wrapper.DeleteInstance();
    }

    [OneTimeTearDown]
    public void Cleanup() =>
        instance.DeleteInstance();
}

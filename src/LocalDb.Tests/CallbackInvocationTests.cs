[TestFixture]
public class CallbackInvocationTests
{
    static DateTime timestamp = new(2000, 1, 1);
    static DateTime differentTimestamp = new(2001, 1, 1);

    [Test]
    public async Task NoInstanceExists_CallbackInvokedOnce()
    {
        // Scenario: LocalDbApi.GetInstance(instance).Exists is false
        var name = "CallbackTest_NoInstance";
        LocalDbApi.StopAndDelete(name);
        DirectoryFinder.Delete(name);

        var callbackCount = 0;
        using var wrapper = new Wrapper(
            name,
            DirectoryFinder.Find(name),
            callback: (connection, cancel) =>
            {
                callbackCount++;
                return VerifyConnectionWorks(connection, cancel);
            });

        wrapper.Start(timestamp, TestDbBuilder.CreateTable);
        await wrapper.AwaitStart();

        // Create two databases from template
        await using (await wrapper.CreateDatabaseFromTemplate("Database1"))
        {
        }
        await using (await wrapper.CreateDatabaseFromTemplate("Database2"))
        {
        }

        AreEqual(1, callbackCount, "Callback should be invoked exactly once");
        wrapper.DeleteInstance();
    }

    [Test]
    public async Task InstanceExistsButNotRunning_CallbackInvokedOnce()
    {
        // Scenario: LocalDbApi.GetInstance(instance).IsRunning is false
        var name = "CallbackTest_NotRunning";
        LocalDbApi.StopAndDelete(name);
        DirectoryFinder.Delete(name);

        // Create the instance but don't start it
        LocalDbApi.CreateInstance(name);

        var callbackCount = 0;
        using var wrapper = new Wrapper(
            name,
            DirectoryFinder.Find(name),
            callback: (connection, cancel) =>
            {
                callbackCount++;
                return VerifyConnectionWorks(connection, cancel);
            });

        wrapper.Start(timestamp, TestDbBuilder.CreateTable);
        await wrapper.AwaitStart();

        // Create two databases from template
        await using (await wrapper.CreateDatabaseFromTemplate("Database1"))
        {
        }
        await using (await wrapper.CreateDatabaseFromTemplate("Database2"))
        {
        }

        AreEqual(1, callbackCount, "Callback should be invoked exactly once");
        wrapper.DeleteInstance();
    }

    [Test]
    public async Task InstanceRunningButNoDataFile_CallbackInvokedOnce()
    {
        // Scenario: File.Exists(DataFile) is false
        var name = "CallbackTest_NoDataFile";
        LocalDbApi.StopAndDelete(name);
        DirectoryFinder.Delete(name);

        // Create and start instance without template files
        LocalDbApi.CreateInstance(name);
        LocalDbApi.StartInstance(name);

        var callbackCount = 0;
        using var wrapper = new Wrapper(
            name,
            DirectoryFinder.Find(name),
            callback: (connection, cancel) =>
            {
                callbackCount++;
                return VerifyConnectionWorks(connection, cancel);
            });

        wrapper.Start(timestamp, TestDbBuilder.CreateTable);
        await wrapper.AwaitStart();

        // Create two databases from template
        await using (await wrapper.CreateDatabaseFromTemplate("Database1"))
        {
        }
        await using (await wrapper.CreateDatabaseFromTemplate("Database2"))
        {
        }

        AreEqual(1, callbackCount, "Callback should be invoked exactly once");
        wrapper.DeleteInstance();
    }

    [Test]
    public async Task TemplateExistsTimestampMatches_CallbackInvokedOnce()
    {
        // Scenario: All true AND timestamp == templateLastMod (no rebuild needed)
        var name = "CallbackTest_TimestampMatches";
        LocalDbApi.StopAndDelete(name);
        DirectoryFinder.Delete(name);

        // First wrapper to create the template
        using (var setupWrapper = new Wrapper(name, DirectoryFinder.Find(name)))
        {
            setupWrapper.Start(timestamp, TestDbBuilder.CreateTable);
            await setupWrapper.AwaitStart();
            // Template now exists with the timestamp
        }

        var callbackCount = 0;
        using var wrapper = new Wrapper(
            name,
            DirectoryFinder.Find(name),
            callback: (connection, cancel) =>
            {
                callbackCount++;
                return VerifyConnectionWorks(connection, cancel);
            });

        // Start with same timestamp - should not rebuild
        wrapper.Start(timestamp, TestDbBuilder.CreateTable);
        await wrapper.AwaitStart();

        // Create two databases from template
        await using (await wrapper.CreateDatabaseFromTemplate("Database1"))
        {
        }
        await using (await wrapper.CreateDatabaseFromTemplate("Database2"))
        {
        }

        AreEqual(1, callbackCount, "Callback should be invoked exactly once");
        wrapper.DeleteInstance();
    }

    [Test]
    public async Task TemplateExistsTimestampDifferent_CallbackInvokedOnce()
    {
        // Scenario: All true AND timestamp != templateLastMod (rebuild needed)
        var name = "CallbackTest_TimestampDifferent";
        LocalDbApi.StopAndDelete(name);
        DirectoryFinder.Delete(name);

        // First wrapper to create the template with original timestamp
        using (var setupWrapper = new Wrapper(name, DirectoryFinder.Find(name)))
        {
            setupWrapper.Start(timestamp, TestDbBuilder.CreateTable);
            await setupWrapper.AwaitStart();
            // Template now exists with the original timestamp
        }

        var callbackCount = 0;
        using var wrapper = new Wrapper(
            name,
            DirectoryFinder.Find(name),
            callback: (connection, cancel) =>
            {
                callbackCount++;
                return VerifyConnectionWorks(connection, cancel);
            });

        // Start with different timestamp - should rebuild
        wrapper.Start(differentTimestamp, TestDbBuilder.CreateTable);
        await wrapper.AwaitStart();

        // Create two databases from template
        await using (await wrapper.CreateDatabaseFromTemplate("Database1"))
        {
        }
        await using (await wrapper.CreateDatabaseFromTemplate("Database2"))
        {
        }

        AreEqual(1, callbackCount, "Callback should be invoked exactly once");
        wrapper.DeleteInstance();
    }

    [Test]
    public async Task MultipleWrapperInstances_EachCallbackInvokedOnce()
    {
        // Verify that multiple wrapper instances each invoke their callback once
        var name = "CallbackTest_MultipleWrappers";
        LocalDbApi.StopAndDelete(name);
        DirectoryFinder.Delete(name);

        var firstCallbackCount = 0;
        var secondCallbackCount = 0;

        // First wrapper
        using (var wrapper1 = new Wrapper(
            name,
            DirectoryFinder.Find(name),
            callback: (connection, cancel) =>
            {
                firstCallbackCount++;
                return VerifyConnectionWorks(connection, cancel);
            }))
        {
            wrapper1.Start(timestamp, TestDbBuilder.CreateTable);
            await wrapper1.AwaitStart();

            await using (await wrapper1.CreateDatabaseFromTemplate("Database1"))
            {
            }
            await using (await wrapper1.CreateDatabaseFromTemplate("Database2"))
            {
            }

            AreEqual(1, firstCallbackCount, "First wrapper callback should be invoked exactly once");
        }

        // Second wrapper with same instance (template exists, timestamp matches)
        using (var wrapper2 = new Wrapper(
            name,
            DirectoryFinder.Find(name),
            callback: (connection, cancel) =>
            {
                secondCallbackCount++;
                return VerifyConnectionWorks(connection, cancel);
            }))
        {
            wrapper2.Start(timestamp, TestDbBuilder.CreateTable);
            await wrapper2.AwaitStart();

            await using (await wrapper2.CreateDatabaseFromTemplate("Database3"))
            {
            }
            await using (await wrapper2.CreateDatabaseFromTemplate("Database4"))
            {
            }

            AreEqual(1, secondCallbackCount, "Second wrapper callback should be invoked exactly once");
            wrapper2.DeleteInstance();
        }
    }

    [Test]
    public async Task NoCallback_NoCrash()
    {
        // Verify that not providing a callback works fine
        var name = "CallbackTest_NoCallback";
        LocalDbApi.StopAndDelete(name);
        DirectoryFinder.Delete(name);

        using var wrapper = new Wrapper(name, DirectoryFinder.Find(name));
        wrapper.Start(timestamp, TestDbBuilder.CreateTable);
        await wrapper.AwaitStart();

        // Should work without callback
        await using (await wrapper.CreateDatabaseFromTemplate("Database1"))
        {
        }
        await using (await wrapper.CreateDatabaseFromTemplate("Database2"))
        {
        }

        wrapper.DeleteInstance();
    }

    static async Task VerifyConnectionWorks(SqlConnection connection, Cancel cancel)
    {
        AreEqual(ConnectionState.Open, connection.State);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM MyTable";
        await command.ExecuteScalarAsync(cancel);
    }
}

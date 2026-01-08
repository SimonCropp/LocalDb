[TestFixture]
public class Tests
{
    [Test]
    public async Task Simple()
    {
        using var instance = new SqlInstance("Name", TestDbBuilder.CreateTable);

        await using var database = await instance.Build();
        var connection = database.Connection;
        var data = await TestDbBuilder.AddData(connection);
        Contains(data, await TestDbBuilder.GetData(connection));
    }

    [Test]
    public async Task ServiceScope()
    {
        static void Add(List<object?> objects, IServiceProvider provider) =>
            objects.Add(provider.GetService<SqlConnection>());

        using var instance = new SqlInstance("ServiceScope", TestDbBuilder.CreateTable);

        await using var database = await instance.Build();
        await using var asyncScope = database.CreateAsyncScope();
        await using var providerAsyncScope = ((IServiceProvider)database).CreateAsyncScope();
        await using var scopeFactoryAsyncScope = ((IServiceScopeFactory)database).CreateAsyncScope();
        using var scope = database.CreateScope();
        var list = new List<object?>();
        Add(list, database);
        Add(list, scope.ServiceProvider);
        Add(list, asyncScope.ServiceProvider);
        Add(list, providerAsyncScope.ServiceProvider);
        Add(list, scopeFactoryAsyncScope.ServiceProvider);

        for (var outerIndex = 0; outerIndex < list.Count; outerIndex++)
        {
            var item = list[outerIndex];
            NotNull(item);
            for (var innerIndex = 0; innerIndex < list.Count; innerIndex++)
            {
                if (innerIndex == outerIndex)
                {
                    continue;
                }
                var nested = list[innerIndex];
                AreNotEqual(item, nested);
            }
        }
    }

    [Test]
    public async Task Callback()
    {
        var callbackCalled = false;
        using var instance = new SqlInstance(
            "Tests_Callback",
            TestDbBuilder.CreateTable,
            callback: _ =>
            {
                callbackCalled = true;
                return Task.CompletedTask;
            });

        await using var database = await instance.Build();
        True(callbackCalled);
    }

    //[Test]
    //public async Task SuppliedTemplate()
    //{
    //    // The template has been pre-created with 2 test entities
    //    var templatePath = Path.Join(AppDomain.CurrentDomain.BaseDirectory, "suppliedTemplate.mdf");
    //    var logPath = Path.Join(AppDomain.CurrentDomain.BaseDirectory, "suppliedTemplate_log.ldf");

    //    var myInstance = new SqlInstance("SuppliedTemplate", TestDbBuilder.CreateTable, templatePath: templatePath, logPath: logPath);
    //    await using var database = await myInstance.Build();
    //    var connection = database.Connection;
    //    //var data = await TestDbBuilder.AddData(connection);
    //    //Assert.Contains(data, await TestDbBuilder.GetData(connection));
    //}

    [Test]
    public async Task Defined_TimeStamp()
    {
        var dateTime = DateTime.Now;
        using var instance = new SqlInstance("Defined_TimeStamp", TestDbBuilder.CreateTable, timestamp: dateTime);

        await using var database = await instance.Build();
        AreEqual(dateTime, File.GetCreationTime(instance.Wrapper.DataFile));
    }

    [Test]
    public async Task Delegate_TimeStamp()
    {
        using var instance = new SqlInstance("Delegate_TimeStamp", TestDbBuilder.CreateTable);

        await using var database = await instance.Build();
        AreEqual(Timestamp.LastModified<Tests>(), File.GetCreationTime(instance.Wrapper.DataFile));
    }

    [Test]
    public async Task Multiple()
    {
        var stopwatch = Stopwatch.StartNew();
        using var instance = new SqlInstance("Multiple", TestDbBuilder.CreateTable);

        await using (await instance.Build(databaseSuffix: "one"))
        {
        }

        await using (await instance.Build(databaseSuffix: "two"))
        {
        }

        await using (await instance.Build(databaseSuffix: "three"))
        {
        }

        Trace.WriteLine(stopwatch.ElapsedMilliseconds);
    }
}

using LocalDb;

[Collection("Sequential")]
public class Tests
{
    [Fact]
    public async Task Simple()
    {
        var instance = new SqlInstance("Name", TestDbBuilder.CreateTable);

        await using var database = await instance.Build();
        var connection = database.Connection;
        var data = await TestDbBuilder.AddData(connection);
        Assert.Contains(data, await TestDbBuilder.GetData(connection));
    }

    [Fact]
    public async Task ServiceScope()
    {
        static void Add(List<object?> objects, IServiceProvider provider)
        {
            objects.Add(provider.GetService<DataSqlConnection>());
            objects.Add(provider.GetService<SqlConnection>());
        }

        var instance = new SqlInstance("ServiceScope", TestDbBuilder.CreateTable);

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
            Assert.NotNull(item);
            for (var innerIndex = 0; innerIndex < list.Count; innerIndex++)
            {
                if (innerIndex == outerIndex)
                {
                    continue;
                }
                var nested = list[innerIndex];
                Assert.NotSame(item, nested);
            }
        }
    }

    [Fact]
    public async Task Callback()
    {
        var callbackCalled = false;
        var instance = new SqlInstance(
            "Tests_Callback",
            TestDbBuilder.CreateTable,
            callback: _ =>
            {
                callbackCalled = true;
                return Task.CompletedTask;
            });

        await using var database = await instance.Build();
        Assert.True(callbackCalled);
    }

    //[Fact]
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

    [Fact]
    public async Task Defined_TimeStamp()
    {
        var dateTime = DateTime.Now;
        var instance = new SqlInstance("Defined_TimeStamp", TestDbBuilder.CreateTable, timestamp: dateTime);

        await using var database = await instance.Build();
        Assert.Equal(dateTime, File.GetCreationTime(instance.Wrapper.DataFile));
    }

    [Fact]
    public async Task Delegate_TimeStamp()
    {
        var instance = new SqlInstance("Delegate_TimeStamp", TestDbBuilder.CreateTable);

        await using var database = await instance.Build();
        Assert.Equal(Timestamp.LastModified<Tests>(), File.GetCreationTime(instance.Wrapper.DataFile));
    }

    [Fact]
    public async Task Multiple()
    {
        var stopwatch = Stopwatch.StartNew();
        var instance = new SqlInstance("Multiple", TestDbBuilder.CreateTable);

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
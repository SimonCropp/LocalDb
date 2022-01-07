using LocalDb;

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
    public async Task Callback()
    {
        var callbackCalled = false;
        var instance = new SqlInstance("Tests_Callback", TestDbBuilder.CreateTable, callback: _ =>
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
        var instance = new SqlInstance(name: "Defined_TimeStamp", buildTemplate: TestDbBuilder.CreateTable, timestamp: dateTime);

        await using var database = await instance.Build();
        Assert.Equal(dateTime, File.GetCreationTime(instance.Wrapper.DataFile));
    }

    [Fact]
    public async Task Delegate_TimeStamp()
    {
        var instance = new SqlInstance(name: "Delegate_TimeStamp", buildTemplate: TestDbBuilder.CreateTable);

        await using var database = await instance.Build();
        Assert.Equal(Timestamp.LastModified<Tests>(), File.GetCreationTime(instance.Wrapper.DataFile));
    }

    [Fact]
    public async Task Multiple()
    {
        var stopwatch = Stopwatch.StartNew();
        var instance = new SqlInstance("Multiple", TestDbBuilder.CreateTable);

        await using (var database = await instance.Build(databaseSuffix: "one"))
        {
        }

        await using (var database = await instance.Build(databaseSuffix: "two"))
        {
        }

        await using (var database = await instance.Build(databaseSuffix: "three"))
        {
        }

        Trace.WriteLine(stopwatch.ElapsedMilliseconds);
    }
}
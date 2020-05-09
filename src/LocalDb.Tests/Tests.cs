using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using LocalDb;
using VerifyXunit;
using Xunit;
using Xunit.Abstractions;

public class Tests :
    VerifyBase
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
        var instance = new SqlInstance(
            name: "Defined_TimeStamp",
            buildTemplate: TestDbBuilder.CreateTable,
            timestamp: dateTime);

        await using var database = await instance.Build();
        Assert.Equal(dateTime, File.GetCreationTime(instance.Wrapper.TemplateDataFile));
    }

    [Fact]
    public async Task Delegate_TimeStamp()
    {
        var instance = new SqlInstance(
            name: "Delegate_TimeStamp",
            buildTemplate: TestDbBuilder.CreateTable);

        await using var database = await instance.Build();
        Assert.Equal(Timestamp.LastModified<Tests>(), File.GetCreationTime(instance.Wrapper.TemplateDataFile));
    }

    [Fact]
    public async Task WithRollback()
    {
        var instance = new SqlInstance("Name", TestDbBuilder.CreateTable);

        await using var database1 = await instance.BuildWithRollback();
        await using var database2 = await instance.BuildWithRollback();
        var data = await TestDbBuilder.AddData(database1.Connection);
        Assert.Contains(data, await TestDbBuilder.GetData(database1.Connection));
        Assert.Empty(await TestDbBuilder.GetData(database2.Connection));
    }

    [Fact]
    public async Task WithRollbackPerf()
    {
        var instance = new SqlInstance("Name", TestDbBuilder.CreateTable);

        await using (await instance.BuildWithRollback())
        {
        }

        SqlDatabaseWithRollback? database = null;

        try
        {
            var stopwatch = Stopwatch.StartNew();
            database = await instance.BuildWithRollback();
            Trace.WriteLine(stopwatch.ElapsedMilliseconds);
            await TestDbBuilder.AddData(database.Connection);
        }
        finally
        {
            var stopwatch = Stopwatch.StartNew();
            database?.Dispose();
            Trace.WriteLine(stopwatch.ElapsedMilliseconds);
        }
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

    //TODO: should duplicate instances throw?
    //[Fact]
    //public void Duplicate()
    //{
    //    Register();
    //    var exception = Assert.Throws<Exception>(Register);
    //    await Verify(exception.Message);
    //}

    //static void Register()
    //{
    //    new SqlInstance("LocalDbDuplicate", TestDbBuilder.CreateTable);
    //}

    public Tests(ITestOutputHelper output) :
        base(output)
    {
    }
}
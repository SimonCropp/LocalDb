using System.Diagnostics;
using System.Threading.Tasks;
using LocalDb;
using Xunit;
using Xunit.Abstractions;

public class Tests :
    XunitLoggingBase
{
    [Fact]
    public async Task Simple()
    {
        var instance = new SqlInstance("Name", TestDbBuilder.CreateTable);

        using (var database = await instance.Build())
        {
            var connection = database.Connection;
            var data = await TestDbBuilder.AddData(connection);
            Assert.Contains(data, await TestDbBuilder.GetData(connection));
            var settings = DbPropertyReader.Read(connection, database.Name);
            Assert.NotEmpty(settings.Files);
        }
    }

    [Fact]
    public async Task Multiple()
    {
        var stopwatch = Stopwatch.StartNew();
        var instance = new SqlInstance("Multiple", TestDbBuilder.CreateTable);

        using (var database = await instance.Build(databaseSuffix: "one"))
        {
           // await database.Delete();
        }

        //await Task.Delay(200);
        //using (var database = await instance.Build(databaseSuffix: "two"))
        //{
        //    // await database.Delete();
        //}

        //using (var database = await instance.Build(databaseSuffix: "three"))
        //{
        //    //await database.Delete();
        //}

        Trace.WriteLine(stopwatch.ElapsedMilliseconds);
    }

    [Fact]
    public async Task NoFileAndNoInstance()
    {
        LocalDbApi.StopAndDelete("NoFileAndNoInstance");
        DirectoryFinder.Delete("NoFileAndNoInstance");

        var instance = new SqlInstance("NoFileAndNoInstance", TestDbBuilder.CreateTable);

        await AddAndVerifyData(instance);
    }

    [Fact]
    public async Task WithFileAndNoInstance()
    {
        var sqlInstance = new SqlInstance("WithFileAndNoInstance", TestDbBuilder.CreateTable);
        sqlInstance.Cleanup();
        var instance = new SqlInstance("WithFileAndNoInstance", TestDbBuilder.CreateTable);

        await AddAndVerifyData(instance);
    }

    [Fact]
    public async Task NoFileAndWithInstance()
    {
        LocalDbApi.CreateInstance("NoFileAndWithInstance");
        DirectoryFinder.Delete("NoFileAndWithInstance");

        var instance = new SqlInstance("NoFileAndWithInstance", TestDbBuilder.CreateTable);

        await AddAndVerifyData(instance);
    }

    //TODO: should duplicate instances throw?
    //[Fact]
    //public void Duplicate()
    //{
    //    Register();
    //    var exception = Assert.Throws<Exception>(Register);
    //    Approvals.Verify(exception.Message);
    //}

    //static void Register()
    //{
    //    new SqlInstance("LocalDbDuplicate", TestDbBuilder.CreateTable);
    //}

    static async Task AddAndVerifyData(SqlInstance instance)
    {
        using (var database = await instance.Build())
        {
            var connection = database.Connection;
            var data = await TestDbBuilder.AddData(connection);
            Assert.Contains(data, await TestDbBuilder.GetData(connection));
        }
    }

    public Tests(ITestOutputHelper output) :
        base(output)
    {
    }
}
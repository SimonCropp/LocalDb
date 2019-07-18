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
        }
    }

    [Fact]
    public async Task Multiple()
    {
        var stopwatch = Stopwatch.StartNew();
        var instance = new SqlInstance("Multiple", TestDbBuilder.CreateTable);

        using (var database = await instance.Build(databaseSuffix: "one"))
        {
        }

        using (var database = await instance.Build(databaseSuffix: "two"))
        {
        }

        using (var database = await instance.Build(databaseSuffix: "three"))
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
    //    Approvals.Verify(exception.Message);
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
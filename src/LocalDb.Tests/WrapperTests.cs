using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using ApprovalTests;
using Xunit;
using Xunit.Abstractions;

public class WrapperTests :
    XunitLoggingBase
{
    static Wrapper instance;

    [Fact]
    public void InvalidInstanceName()
    {
        var exception = Assert.Throws<ArgumentException>(() => new Wrapper("<", "s", 4));
        Approvals.Verify(exception.Message);
    }

    [Fact]
    public async Task Delete()
    {
        var (connection, id) = await instance.CreateDatabaseFromTemplate("ToDelete");
        await instance.DeleteDatabase("ToDelete");

        using (var sqlConnection = new SqlConnection(instance.MasterConnectionString))
        {
            await sqlConnection.OpenAsync();
            var settings = DbPropertyReader.Read(sqlConnection, id);
            Assert.Empty(settings.Files);
        }
    }

    public WrapperTests(ITestOutputHelper output) :
        base(output)
    {
    }

    static WrapperTests()
    {
        instance = new Wrapper("WrapperTests", DirectoryFinder.Find("WrapperTests"), 4);
        instance.Start(DateTime.Now, TestDbBuilder.CreateTable);
    }
}
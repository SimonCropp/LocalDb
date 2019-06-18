using System.Diagnostics;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

public class SqlLocalDbTests :
    XunitLoggingBase
{
    [Fact]
    public void Instances()
    {
        var collection = SqlLocalDb.Instances().ToList();
        foreach (var instance in collection)
        {
            Trace.WriteLine(instance);
        }
        Assert.NotEmpty(collection);
    }

    public SqlLocalDbTests(ITestOutputHelper output) :
        base(output)
    {
    }
}
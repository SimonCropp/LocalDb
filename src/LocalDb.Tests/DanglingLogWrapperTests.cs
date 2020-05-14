using System;
using VerifyXunit;
using Microsoft.Data.SqlClient;
using Xunit;
using Xunit.Abstractions;

public class DanglingLogWrapperTests :
    VerifyBase
{
    [Fact]
    public void Run()
    {
        var name = "DanglingLogWrapperTests";
        LocalDbApi.StopAndDelete(name);
        var instance = new Wrapper(s => new SqlConnection(s), name, DirectoryFinder.Find(name));
        base.Dispose();
        instance.Start(DateTime.Now, TestDbBuilder.CreateTable);
    }

    public DanglingLogWrapperTests(ITestOutputHelper output) :
        base(output)
    {
    }

}
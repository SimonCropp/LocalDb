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
        LocalDbApi.StopAndDelete("DanglingLogWrapperTests");
        var instance = new Wrapper(s => new SqlConnection(s), "WrapperTests", DirectoryFinder.Find("DanglingLogWrapperTests"));
        base.Dispose();
        instance.Start(DateTime.Now, TestDbBuilder.CreateTable);
    }

    public DanglingLogWrapperTests(ITestOutputHelper output) :
        base(output)
    {
    }

}
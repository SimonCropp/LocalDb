using System;
using ApprovalTests;
using Xunit;
using Xunit.Abstractions;

public class ExceptionBuilderTests :
    XunitLoggingBase
{
    [Fact]
    public void WrapLocalDbFailure()
    {
        var wrapped = ExceptionBuilder.WrapLocalDbFailure("InstanceName", @"c:\LocalDBData\InstanceName", new Exception());
        Approvals.Verify(wrapped.Message);
    }

    public ExceptionBuilderTests(ITestOutputHelper output) :
        base(output)
    {
    }
}
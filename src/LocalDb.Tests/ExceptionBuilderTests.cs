using System;
using ObjectApproval;
using Xunit;
using Xunit.Abstractions;

public class ExceptionBuilderTests :
    XunitLoggingBase
{
    [Fact]
    public void WrapLocalDbFailure()
    {
        var wrapped = ExceptionBuilder.WrapLocalDbFailure("InstanceName", @"c:\LocalDBData\InstanceName", new Exception());
        ObjectApprover.VerifyWithJson(wrapped);
    }
    [Fact]
    public void WrapLocalDbFailure_message()
    {
        var wrapped = ExceptionBuilder.WrapLocalDbFailure("InstanceName", @"c:\LocalDBData\InstanceName", new Exception());
        ApprovalTests.Approvals.Verify(wrapped.Message);
    }

    public ExceptionBuilderTests(ITestOutputHelper output) :
        base(output)
    {
    }
}
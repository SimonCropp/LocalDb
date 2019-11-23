using System;
using System.Threading.Tasks;
using VerifyXunit;
using Xunit;
using Xunit.Abstractions;

public class ExceptionBuilderTests :
    VerifyBase
{
    [Fact]
    public Task WrapLocalDbFailure()
    {
        var wrapped = ExceptionBuilder.WrapLocalDbFailure("InstanceName", @"c:\LocalDBData\InstanceName", new Exception());
        return Verify(wrapped.Message);
    }

    public ExceptionBuilderTests(ITestOutputHelper output) :
        base(output)
    {
    }
}
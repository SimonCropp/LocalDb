﻿using System.Threading.Tasks;
using VerifyXunit;
using Xunit;

[UsesVerify]
public class ExceptionBuilderTests
{
    [Fact]
    public Task WrapLocalDbFailure()
    {
        var wrapped = ExceptionBuilder.WrapLocalDbFailure("InstanceName", @"c:\LocalDBData\InstanceName", new());
        return Verifier.Verify(wrapped.Message);
    }
}
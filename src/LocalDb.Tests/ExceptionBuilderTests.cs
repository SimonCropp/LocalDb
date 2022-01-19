[UsesVerify]
public class ExceptionBuilderTests
{
    [Fact]
    public Task WrapLocalDbFailure()
    {
        var wrapped = ExceptionBuilder.WrapLocalDbFailure("InstanceName", @"c:\LocalDBData\InstanceName", new());
        return Verify(wrapped.Message);
    }
}
public class ExceptionBuilderTests
{
    [Test]
    public Task WrapLocalDbFailure()
    {
        var wrapped = ExceptionBuilder.WrapLocalDbFailure("InstanceName", @"c:\LocalDBData\InstanceName", new());
        return Verify(wrapped.Message);
    }
}
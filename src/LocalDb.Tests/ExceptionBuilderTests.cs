[TestFixture]
public class ExceptionBuilderTests
{
    [Test]
    public Task WrapLocalDbFailure()
    {
        var wrapped = ExceptionBuilder.WrapLocalDbFailure("InstanceName", @"c:\LocalDBData\InstanceName", new());
        return Verify(wrapped.Message)
            .Snapshot(
                """
                Failed to setup a LocalDB instance.
                name: InstanceName
                directory: c:\LocalDBData\InstanceName:

                To cleanup perform the following actions:
                 * Execute 'sqllocaldb stop InstanceName'
                 * Execute 'sqllocaldb delete InstanceName'
                 * Delete the directory c:\LocalDBData\InstanceName'
                """);
    }
}
static class ExceptionBuilder
{
    public static Exception WrapLocalDbFailure(string name, string directory, Exception exception)
    {
        var message = $@"Failed to setup a LocalDB instance.
{nameof(name)}: {name}
{nameof(directory)}: {directory}:

To cleanup perform the following actions:
 * Execute 'sqllocaldb stop {name}'
 * Execute 'sqllocaldb delete {name}'
 * Delete the directory {directory}'
";
        return new Exception(message, exception);
    }
}
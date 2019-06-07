using System;

static class ExceptionBuilder
{
    public static void WrapAndThrowLocalDbFailure(string name, string directory, Exception exception)
    {
        var message = $@"Failed to setup a LocalDB instance.
{nameof(name)}: {name}
{nameof(directory)}: {directory}:

To cleanup perform the following actions:
 * Execute 'sqllocaldb stop {name}'
 * Execute 'sqllocaldb delete {name}'
 * Delete the directory {directory}'
";
        throw new Exception(message, exception);
    }
}
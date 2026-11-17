static class ExceptionBuilder
{
    public static Exception WrapLocalDbFailure(string name, string directory, Exception exception)
    {
        var message = $"""
            Failed to setup a LocalDB instance.
            {nameof(name)}: {name}
            {nameof(directory)}: {directory}:
            
            To cleanup perform the following actions:
             * Execute 'sqllocaldb stop {name}'
             * Execute 'sqllocaldb delete {name}'
             * Delete the directory {directory}'
            """;
        return new(message, exception);
    }

    // A failed create can leave a half started sqlservr, so the cleanup stops the instance for
    // both operations
    public static Exception RetryFailure(string operation, string name, int result, int attempts)
    {
        var message = $"""
            Failed to {operation} LocalDB instance after {attempts} attempts.
            {nameof(name)}: {name}
            HRESULT: {StartRetry.ToHex(result)}

            To cleanup perform the following actions:
             * Execute 'sqllocaldb stop {name} -k'
             * Execute 'sqllocaldb delete {name}'
             * Delete the instance data directory
            """;
        return new(message);
    }
}
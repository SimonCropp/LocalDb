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
    // both operations. errorLog, the tail of the instance's error log, goes last: it is the
    // longest part and the cleanup steps should not be buried under it
    public static Exception RetryFailure(string operation, string name, int result, int attempts, string? errorLog)
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
        if (errorLog is null)
        {
            return new(message);
        }

        return new($"""
            {message}

            {errorLog}
            """);
    }
}
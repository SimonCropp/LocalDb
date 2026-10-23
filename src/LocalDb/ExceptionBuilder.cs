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

    public static Exception StartFailure(string name, int result, int attempts)
    {
        var message = $"""
            Failed to start LocalDB instance after {attempts} attempts.
            {nameof(name)}: {name}
            HRESULT: {StartRetry.ToHex(result)}

            To cleanup perform the following actions:
             * Execute 'sqllocaldb stop {name} -k'
             * Execute 'sqllocaldb delete {name}'
             * Delete the instance data directory
            """;
        return new(message);
    }

    public static Exception CreateFailure(string name, int result)
    {
        var message = $"""
            Failed to create LocalDB instance.
            {nameof(name)}: {name}
            HRESULT: {StartRetry.ToHex(result)}

            To cleanup perform the following actions:
             * Execute 'sqllocaldb delete {name}'
             * Delete the instance data directory
            """;
        return new(message);
    }
}
static class StartRetry
{
    public const int MaxAttempts = 3;

    // LOCALDB_ERROR_SQL_SERVER_STARTUP_FAILED. Seen on CI runners where the sqlservr process
    // intermittently fails to start. LocalDBCreateInstance starts sqlservr to initialize a new
    // instance, so it returns this too: on a fresh runner, several instances created at once can
    // all fail with it
    public const int SqlServerStartupFailed = unchecked((int)0x89C5010A);

    // Retries a failing LocalDB call that starts sqlservr: LocalDBCreateInstance or
    // LocalDBStartInstance. operation names the call ("create" or "start") in the log and the
    // exception. Between attempts recover is called so the caller can kill any half started
    // process (and optionally recreate the instance). If another process started the instance in
    // the meantime, that is treated as success. readErrorLog is only called once every attempt
    // has failed, and its result goes in the exception: the HRESULT alone does not say why
    // sqlservr failed to start.
    public static void Run(
        string instanceName,
        string operation,
        Func<int> action,
        Func<bool> isRunning,
        Action recover,
        Func<string?> readErrorLog,
        Action<TimeSpan> sleep)
    {
        var result = 0;
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            result = action();
            if (result >= 0)
            {
                return;
            }

            if (isRunning())
            {
                return;
            }

            LocalDbLogging.Log($"Failed to {operation} LocalDB instance `{instanceName}` (attempt {attempt} of {MaxAttempts}). HRESULT: {ToHex(result)}");

            if (attempt == MaxAttempts)
            {
                break;
            }

            recover();
            sleep(Delay(attempt));
        }

        throw ExceptionBuilder.RetryFailure(operation, instanceName, result, MaxAttempts, readErrorLog());
    }

    // Between attempt and twice attempt seconds. Callers that failed together, such as instances
    // created at once by parallel tests, would otherwise all retry at the same moment and collide
    // again
    public static TimeSpan Delay(int attempt) =>
        TimeSpan.FromMilliseconds(attempt * 1000 + Random.Shared.Next(attempt * 1000));

    public static string ToHex(int result) => $"0x{result:X8}";
}

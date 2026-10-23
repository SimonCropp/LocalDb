static class StartRetry
{
    public const int MaxAttempts = 3;

    // LOCALDB_ERROR_SQL_SERVER_STARTUP_FAILED. Seen on CI runners where the sqlservr process
    // intermittently fails to start
    public const int SqlServerStartupFailed = unchecked((int)0x89C5010A);

    // Retries a failing LocalDBStartInstance. Between attempts recover is called so the caller can
    // kill any half started process (and optionally recreate the instance). If another process
    // started the instance in the meantime, that is treated as success.
    public static void Run(
        string instanceName,
        Func<int> start,
        Func<bool> isRunning,
        Action recover,
        Action<TimeSpan> sleep)
    {
        var result = 0;
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            result = start();
            if (result >= 0)
            {
                return;
            }

            if (isRunning())
            {
                return;
            }

            LocalDbLogging.Log($"Failed to start LocalDB instance `{instanceName}` (attempt {attempt} of {MaxAttempts}). HRESULT: {ToHex(result)}");

            if (attempt == MaxAttempts)
            {
                break;
            }

            recover();
            sleep(TimeSpan.FromSeconds(attempt));
        }

        throw ExceptionBuilder.StartFailure(instanceName, result, MaxAttempts);
    }

    public static string ToHex(int result) => $"0x{result:X8}";
}

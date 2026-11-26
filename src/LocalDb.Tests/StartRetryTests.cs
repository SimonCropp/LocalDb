[TestFixture]
public class StartRetryTests
{
    [Test]
    public void SucceedsFirstAttempt()
    {
        var starts = 0;
        var recovers = 0;
        StartRetry.Run(
            "Instance",
            "start",
            () =>
            {
                starts++;
                return 0;
            },
            () => false,
            () => recovers++,
            () => null,
            _ => { });
        AreEqual(1, starts);
        AreEqual(0, recovers);
    }

    [Test]
    public void RecoversAndRetries()
    {
        var starts = 0;
        var recovers = 0;
        var sleeps = new List<TimeSpan>();
        StartRetry.Run(
            "Instance",
            "start",
            () =>
            {
                starts++;
                return starts < 3 ? StartRetry.SqlServerStartupFailed : 0;
            },
            () => false,
            () => recovers++,
            () => throw new("The error log is only read once every attempt has failed"),
            sleeps.Add);
        AreEqual(3, starts);
        AreEqual(2, recovers);
        AreEqual(2, sleeps.Count);
        AssertDelay(1, sleeps[0]);
        AssertDelay(2, sleeps[1]);
    }

    [Test]
    public void DelayIsJittered()
    {
        var delays = Enumerable.Range(0, 100)
            .Select(_ => StartRetry.Delay(1))
            .ToList();
        foreach (var delay in delays)
        {
            AssertDelay(1, delay);
        }

        // 100 draws from 1000 values all landing on one is not a realistic outcome
        Greater(delays.Distinct().Count(), 1);
    }

    static void AssertDelay(int attempt, TimeSpan delay)
    {
        GreaterOrEqual(delay, TimeSpan.FromSeconds(attempt));
        Less(delay, TimeSpan.FromSeconds(attempt * 2));
    }

    [Test]
    public void FailureButRunningIsSuccess()
    {
        var recovers = 0;
        StartRetry.Run(
            "Instance",
            "start",
            () => StartRetry.SqlServerStartupFailed,
            () => true,
            () => recovers++,
            () => null,
            _ => { });
        AreEqual(0, recovers);
    }

    [Test]
    public Task ThrowsAfterMaxAttempts()
    {
        var starts = 0;
        var exception = Throws<Exception>(
            () => StartRetry.Run(
                "Instance",
                "start",
                () =>
                {
                    starts++;
                    return StartRetry.SqlServerStartupFailed;
                },
                () => false,
                () => { },
                () => null,
                _ => { }))!;
        AreEqual(StartRetry.MaxAttempts, starts);
        return Verify(exception.Message)
            .Snapshot(
                """
                Failed to start LocalDB instance after 3 attempts.
                name: Instance
                HRESULT: 0x89C5010A

                To cleanup perform the following actions:
                 * Execute 'sqllocaldb stop Instance -k'
                 * Execute 'sqllocaldb delete Instance'
                 * Delete the instance data directory
                """);
    }

    [Test]
    public Task CreateThrowsAfterMaxAttempts()
    {
        var creates = 0;
        var recovers = 0;
        var exception = Throws<Exception>(
            () => StartRetry.Run(
                "Instance",
                "create",
                () =>
                {
                    creates++;
                    return StartRetry.SqlServerStartupFailed;
                },
                () => false,
                () => recovers++,
                () => null,
                _ => { }))!;
        AreEqual(StartRetry.MaxAttempts, creates);
        AreEqual(StartRetry.MaxAttempts - 1, recovers);
        return Verify(exception.Message)
            .Snapshot(
                """
                Failed to create LocalDB instance after 3 attempts.
                name: Instance
                HRESULT: 0x89C5010A

                To cleanup perform the following actions:
                 * Execute 'sqllocaldb stop Instance -k'
                 * Execute 'sqllocaldb delete Instance'
                 * Delete the instance data directory
                """);
    }

    [Test]
    public Task ThrowsWithErrorLog()
    {
        var reads = 0;
        var exception = Throws<Exception>(
            () => StartRetry.Run(
                "Instance",
                "start",
                () => StartRetry.SqlServerStartupFailed,
                () => false,
                () => { },
                () =>
                {
                    reads++;
                    return """
                        Last 2 lines of error.log:
                        Error: 17204, Severity: 16, State: 1.
                        FCB::Open failed: Could not open file master.mdf
                        """;
                },
                _ => { }))!;
        AreEqual(1, reads);
        return Verify(exception.Message);
    }
}

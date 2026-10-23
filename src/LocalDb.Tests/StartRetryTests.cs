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
            () =>
            {
                starts++;
                return 0;
            },
            () => false,
            () => recovers++,
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
            () =>
            {
                starts++;
                return starts < 3 ? StartRetry.SqlServerStartupFailed : 0;
            },
            () => false,
            () => recovers++,
            sleeps.Add);
        AreEqual(3, starts);
        AreEqual(2, recovers);
        AreEqual(new[] { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2) }, sleeps);
    }

    [Test]
    public void FailureButRunningIsSuccess()
    {
        var recovers = 0;
        StartRetry.Run(
            "Instance",
            () => StartRetry.SqlServerStartupFailed,
            () => true,
            () => recovers++,
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
                () =>
                {
                    starts++;
                    return StartRetry.SqlServerStartupFailed;
                },
                () => false,
                () => { },
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
}

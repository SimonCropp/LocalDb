// Mutates LocalDbSettings.PoolSize, which is a global, so it must not overlap other fixtures.
[TestFixture]
[NonParallelizable]
public class PooledFillFailureTests
{
    static DateTime timestamp = new(2000, 1, 1);

    [Test]
    public async Task FillFailureWakesWaitersInsteadOfHanging()
    {
        var name = "PooledFillFailure";
        LocalDbApi.StopAndDelete(name);
        DirectoryFinder.Delete(name);

        var originalPoolSize = LocalDbSettings.PoolSize;
        LocalDbSettings.PoolSize = 3;
        try
        {
            using var wrapper = new Wrapper(name, DirectoryFinder.Find(name));
            wrapper.Start(timestamp, TestDbBuilder.CreateTable);
            await wrapper.AwaitStart();

            // Pooled1 is built normally. Pooled2 stalls in the fill until the test has a
            // second lease parked on the semaphore, then faults.
            var faultReached = new TaskCompletionSource();
            var releaseFault = new TaskCompletionSource();
            var built = 0;

            async Task Initialize(SqlConnection connection)
            {
                if (Interlocked.Increment(ref built) == 1)
                {
                    return;
                }

                faultReached.TrySetResult();
                await releaseFault.Task;
                throw new("Injected pool fill failure");
            }

            // Returns once Pooled1 exists, rather than once the whole pool does.
            var (first, firstName) = await wrapper.OpenPooledDatabase(Initialize);
            AreEqual("Pooled1", firstName);
            await faultReached.Task;

            // Pooled1 is leased and nothing else is built yet, so this parks on poolLease.
            // That is the state a background failure has to be able to wake.
            var blocked = wrapper.OpenPooledDatabase();
            var early = await Task.WhenAny(blocked, Task.Delay(500));
            AreNotSame(blocked, early, "second lease should still be waiting on an unbuilt database");

            releaseFault.SetResult();

            var finished = await Task.WhenAny(blocked, Task.Delay(TimeSpan.FromSeconds(30)));
            AreSame(blocked, finished, "the parked lease was never woken by the fill failure");

            var waiter = ThrowsAsync<Exception>(async () => await blocked)!;
            AreEqual("Failed to build the pooled databases in the background.", waiter.Message);
            AreEqual("Injected pool fill failure", waiter.InnerException!.Message);

            // A lease arriving after the failure fails fast rather than waiting.
            var later = ThrowsAsync<Exception>(async () => await wrapper.OpenPooledDatabase())!;
            AreEqual("Failed to build the pooled databases in the background.", later.Message);
            AreEqual("Injected pool fill failure", later.InnerException!.Message);

            await first.DisposeAsync();
            wrapper.DeleteInstance();
        }
        finally
        {
            LocalDbSettings.PoolSize = originalPoolSize;
        }
    }
}

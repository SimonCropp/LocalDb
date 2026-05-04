// Reproduces the race observed when two callers share a LocalDB user instance and both invoke
// Wrapper.Start with no template on disk. There are actually two distinct overlapping races:
//
//   1. SQL-level DDL deadlock (the one this test catches deterministically). Both wrappers open
//      master connections and run CREATE DATABASE [template] + ALTER + sp_detach_db concurrently;
//      SQL Server picks one as the deadlock victim and throws SqlException 1205. This fires
//      reliably even within a single process — Wrapper.semaphoreSlim is declared but never used
//      to gate InnerStart, so two Wrappers for the same instance have no in-process serialization.
//
//   2. LocalDB-API instance-state race (only when callers are separate Windows processes).
//      Each process independently calls LocalDbApi.StopAndDelete + CreateInstance + StartInstance;
//      one process's StopAndDelete deletes the instance the other is trying to use, surfacing as
//      LOCALDB_ERROR_INSTANCE_DOES_NOT_EXIST (native 0x89C50107) or instance-busy errors.
//      MultiProcessConcurrentStartTests + InstanceDoesNotExistRaceTests cover that variant.
//
// Both races dissolve under the same fix: serialize Wrapper.InnerStart per-instance — an in-process
// lock (e.g. wiring up the existing semaphoreSlim) covers race #1, and a named OS mutex keyed by
// instance name covers race #2.

[TestFixture]
public class ConcurrentStartTests
{
    static readonly DateTime Timestamp = new(2000, 1, 1);

    [Test]
    public async Task ConcurrentStartWithMissingTemplateShouldNotRace()
    {
        const string name = "ConcurrentStartTest";
        const int iterations = 5;
        var directory = DirectoryFinder.Find(name);
        var failures = new List<(int Iteration, Exception Exception)>();

        for (var iteration = 0; iteration < iterations; iteration++)
        {
            // Set up the precondition that triggers the race:
            //  1. The LocalDB user instance EXISTS and is running.
            //  2. The wrapper's data directory has no template.mdf / template_log.ldf.
            // Under that combination, every Wrapper.Start hits the StopAndDelete + CleanStart branch.
            LocalDbApi.StopAndDelete(name);
            DirectoryFinder.Delete(name);
            LocalDbApi.CreateInstance(name);
            LocalDbApi.StartInstance(name);

            try
            {
                using var wrapperA = new Wrapper(name, directory);
                using var wrapperB = new Wrapper(name, directory);

                await Task.WhenAll(
                    Task.Run(async () =>
                    {
                        wrapperA.Start(Timestamp, TestDbBuilder.CreateTable);
                        await wrapperA.AwaitStart();
                    }),
                    Task.Run(async () =>
                    {
                        wrapperB.Start(Timestamp, TestDbBuilder.CreateTable);
                        await wrapperB.AwaitStart();
                    }));
            }
            catch (Exception exception)
            {
                failures.Add((iteration, exception));
            }
        }

        LocalDbApi.StopAndDelete(name);
        DirectoryFinder.Delete(name);

        if (failures.Count > 0)
        {
            static string Describe(Exception exception)
            {
                var current = exception;
                while (current.InnerException != null)
                {
                    current = current.InnerException;
                }
                return $"{current.GetType().Name}: {current.Message.Split('\n')[0]}";
            }

            var summary = string.Join(
                Environment.NewLine,
                failures.Select(f => $"  iteration {f.Iteration}: {f.Exception.GetType().Name}: {f.Exception.Message.Split('\n')[0]} → innermost: {Describe(f.Exception)}"));

            Assert.Fail(
                $"{failures.Count}/{iterations} concurrent Wrapper.Start iterations failed:{Environment.NewLine}{summary}");
        }
    }
}

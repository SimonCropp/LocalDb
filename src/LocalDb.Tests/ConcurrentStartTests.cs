// Demonstrates how the AiCliDetector prefix scheme isolates an AI session from a human session
// when both are running tests against LocalDB. With the prefix in place, an AI process targets
// (LocalDb)\chatbot_X and a human process targets (LocalDb)\X — different LocalDB user
// instances, no shared metadata, no shared template directory, no race.
//
// Without the prefix, two callers sharing a single LocalDB user instance with no template on
// disk would race in two ways:
//
//   1. SQL-level DDL deadlock. Both wrappers open master connections and run
//      CREATE DATABASE [template] + ALTER + sp_detach_db concurrently; SQL Server picks one
//      as the deadlock victim and throws SqlException 1205. Fires reliably even within a
//      single process.
//
//   2. LocalDB-API instance-state race (only when callers are separate Windows processes).
//      Each process independently calls LocalDbApi.StopAndDelete + CreateInstance +
//      StartInstance; one process's StopAndDelete deletes the instance the other is trying
//      to use, surfacing as LOCALDB_ERROR_INSTANCE_DOES_NOT_EXIST (native 0x89C50107) or
//      instance-busy errors. MultiProcessConcurrentStartTests covers that variant.
//
// This test sets the CLAUDECODE env var so AiCliDetector reports an AI session, then
// constructs one wrapper with the unprefixed (human) name and one with the chatbot_-prefixed
// (AI) name. They target different LocalDB instances and run concurrently without racing.

[TestFixture]
public class ConcurrentStartTests
{
    static readonly DateTime timestamp = new(2000, 1, 1);

    [Test]
    public async Task ConcurrentStartWithMissingTemplateShouldNotRace()
    {
        Environment.SetEnvironmentVariable("CLAUDECODE", "1");
        try
        {
            const string humanName = "ConcurrentStartTest";
            var aiName = "chatbot_" + humanName;
            const int iterations = 5;
            var humanDir = DirectoryFinder.Find(humanName);
            var aiDir = DirectoryFinder.Find(aiName);
            var failures = new List<(int Iteration, Exception Exception)>();

            for (var iteration = 0; iteration < iterations; iteration++)
            {
                // Pre-condition for both instances: instance exists and is running, but the
                // template files are missing. This forces Wrapper.Start down the
                // StopAndDelete + CleanStart branch — the most race-prone path.
                foreach (var instanceName in new[] { humanName, aiName })
                {
                    LocalDbApi.StopAndDelete(instanceName);
                    DirectoryFinder.Delete(instanceName);
                    LocalDbApi.CreateInstance(instanceName);
                    LocalDbApi.StartInstance(instanceName);
                }

                try
                {
                    using var wrapperHuman = new Wrapper(humanName, humanDir);
                    using var wrapperAi = new Wrapper(aiName, aiDir);

                    await Task.WhenAll(
                        Task.Run(async () =>
                        {
                            wrapperHuman.Start(timestamp, TestDbBuilder.CreateTable);
                            await wrapperHuman.AwaitStart();
                        }),
                        Task.Run(async () =>
                        {
                            wrapperAi.Start(timestamp, TestDbBuilder.CreateTable);
                            await wrapperAi.AwaitStart();
                        }));
                }
                catch (Exception exception)
                {
                    failures.Add((iteration, exception));
                }
            }

            foreach (var instanceName in new[] { humanName, aiName })
            {
                LocalDbApi.StopAndDelete(instanceName);
                DirectoryFinder.Delete(instanceName);
            }

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

                Fail(
                    $"{failures.Count}/{iterations} concurrent Wrapper.Start iterations failed:{Environment.NewLine}{summary}");
            }
        }
        finally
        {
            Environment.SetEnvironmentVariable("CLAUDECODE", null);
        }
    }
}

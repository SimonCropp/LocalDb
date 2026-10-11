/// <summary>
/// Measures Query Store on a cloned database.
///
/// SQL Server 2022 turns Query Store on for every new database, so the template gets it and every
/// clone inherits it. On a test database that buys nothing: its job is to keep query, plan and
/// runtime history across restarts, and a test database is gone before anyone could read it. What
/// it costs is work nobody asked for - on each clone, about half a second after it is first
/// opened, Query Store writes its own tables (around 170 log records), and from then on it tracks
/// what is executed.
///
/// The setting is made on the template in buildTemplate, and GlobalSetup refuses to run unless the
/// clone reports the same state, so the numbers are for the mechanism a template-level change
/// would actually use.
///
/// Three workloads: a test's worth of work against a database of its own (where the per-clone
/// write lands), many statements each compiled once, and one statement repeated often enough to
/// cross the auto capture threshold.
///
/// Result, over three runs: off is faster where a database is cloned and used (43-47 ms against
/// 47-52 ms, so 6-10%, in two runs) and where statements are each compiled once (77-83 ms against
/// 82-91 ms, 5-16%). One statement repeated shows no consistent difference. With it on the timings
/// are also less steady: the other run of the clone workload averaged 80 ms, with iterations out
/// to 200 ms. The diagnoser's I/O figures say nothing here - they are totals for a run rather than
/// per operation, and a database that has closed drops out of the DMV they are summed from.
/// </summary>
[MemoryDiagnoser]
[WarmupCount(3)]
[IterationCount(15)]
[GcServer(true)]
[SuppressMessage("Performance", "CA1822:Mark members as static")]
public class QueryStoreBenchmarks
{
    const string InstanceName = "Benchmark";
    const int Seed = 1000;
    const int Statements = 100;

    SqlInstance? sqlInstance;
    SqlConnection? connection;
    int databaseCounter;
    int statementCounter;

    [Params(true, false)]
    public bool QueryStore { get; set; }

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        LocalDbApi.StopAndDelete(InstanceName);
        DirectoryFinder.Delete(InstanceName);

        sqlInstance = new(name: InstanceName, buildTemplate: BuildTemplate);
        await sqlInstance.Wrapper.AwaitStart();

        var database = await sqlInstance.Build("QueryStore");
        connection = database.Connection;

        var expected = QueryStore ? "READ_WRITE" : "OFF";
        var actual = await ReadState(connection);
        if (actual != expected)
        {
            throw new($"The template's Query Store setting did not reach the clone. Expected {expected}, the clone reports {actual}.");
        }
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        sqlInstance?.Cleanup(ShutdownMode.KillProcess);
        sqlInstance?.Dispose();
        sqlInstance = null;
    }

    /// <summary>
    /// One test: a database of its own, a write, a read. Each invocation leaves its database
    /// attached, as a test run does, so the write Query Store makes after a clone is opened lands
    /// while later invocations are running.
    /// </summary>
    [Benchmark]
    public async Task BuildInsertAndQuery()
    {
        var name = $"QueryStore{Interlocked.Increment(ref databaseCounter)}";
        await using var database = await sqlInstance!.Build(name);
        await Execute(database.Connection, "insert dbo.Rows (Value) values (N'inserted');");
        await Execute(database.Connection, "select count(*) from dbo.Rows where Value = N'inserted';");
    }

    /// <summary>
    /// Statements that are each new to the server, which is what most of a test suite is: the
    /// alias makes every text distinct, so each one is compiled and each one is new to Query Store.
    /// </summary>
    [Benchmark]
    public async Task DistinctStatements()
    {
        for (var i = 0; i < Statements; i++)
        {
            var number = Interlocked.Increment(ref statementCounter);
            await Execute(connection!, $"select count(*) as Count{number} from dbo.Rows where Value like N'%{number}%';");
        }
    }

    /// <summary>
    /// One statement, repeated. Past thirty executions the auto capture mode starts keeping it, so
    /// after warmup every execution here is one Query Store records.
    /// </summary>
    [Benchmark]
    public async Task RepeatedStatement()
    {
        for (var i = 0; i < Statements; i++)
        {
            await Execute(connection!, "select count(*) from dbo.Rows where Value = N'seeded';");
        }
    }

    async Task BuildTemplate(SqlConnection template)
    {
        await Execute(template,
            """
            create table dbo.Rows
            (
                Id int identity primary key,
                Value nvarchar(100) not null
            );
            """);
        await Execute(template,
            $"""
             insert dbo.Rows (Value)
             select top ({Seed}) N'seeded'
             from sys.all_objects a
             cross join sys.all_objects b;
             """);

        if (QueryStore)
        {
            await Execute(template, "alter database current set query_store = on (operation_mode = read_write);");
        }
        else
        {
            await Execute(template, "alter database current set query_store = off;");
        }
    }

    static async Task<string> ReadState(SqlConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "select actual_state_desc from sys.database_query_store_options;";
        return (string) (await command.ExecuteScalarAsync())!;
    }

    static async Task Execute(SqlConnection connection, string commandText)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = commandText;
        await command.ExecuteNonQueryAsync();
    }
}

/// <summary>
/// Compares a database per test against leasing from a fixed pool.
///
/// The plan cache is keyed by database id, so a fresh database always starts with a cold cache
/// and every statement it runs is compiled from scratch. A test suite that creates one database
/// per test therefore recompiles the same queries once per test, and never reuses a plan. The
/// per-test cost is that compilation plus the file copy and attach for the new database.
///
/// Leasing from a pool of N databases instead means each database is created once, and after the
/// first lease its plans stay cached for every later lease. The template here deliberately holds
/// a view that is cheap to execute but expensive to plan (a wide UNION ALL over several tables),
/// which is what makes the difference visible: the work is the compile, not the query.
///
/// Both arms do the same logical thing — obtain an isolated database, run the query, hand it
/// back — so the difference is the cost of that isolation.
/// </summary>
[MemoryDiagnoser]
[WarmupCount(2)]
[IterationCount(10)]
[GcServer(true)]
[SuppressMessage("Performance", "CA1822:Mark members as static")]
public class PooledDatabaseBenchmarks
{
    const string instanceName = "BenchPooled";
    const int tableCount = 12;
    const int unionBranches = 40;
    SqlInstance? sqlInstance;
    int counter;

    /// <summary>
    /// Tests simulated per benchmark iteration.
    /// </summary>
    [Params(10)]
    public int Tests { get; set; }

    [GlobalSetup]
    public Task GlobalSetup()
    {
        LocalDbApi.StopAndDelete(instanceName);
        DirectoryFinder.Delete(instanceName);

        LocalDbSettings.PoolSize = 4;
        sqlInstance = new(name: instanceName, buildTemplate: BuildTemplate);
        return sqlInstance.Wrapper.AwaitStart();
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        sqlInstance?.Cleanup(ShutdownMode.KillProcess);
        sqlInstance?.Dispose();
        sqlInstance = null;
    }

    /// <summary>
    /// The current model: a fresh database per test, deleted afterwards.
    /// </summary>
    [Benchmark(Baseline = true)]
    public async Task DatabasePerTest()
    {
        for (var index = 0; index < Tests; index++)
        {
            var name = $"PerTest{Interlocked.Increment(ref counter)}";
            var connection = await sqlInstance!.Wrapper.CreateDatabaseFromTemplate(name);
            try
            {
                await Query(connection);
            }
            finally
            {
                await connection.DisposeAsync();
                await sqlInstance.Wrapper.DeleteDatabase(name);
            }
        }
    }

    /// <summary>
    /// The pooled model: lease one of N databases, roll back, hand it back.
    /// </summary>
    [Benchmark]
    public async Task PooledDatabase()
    {
        for (var index = 0; index < Tests; index++)
        {
            var (connection, name) = await sqlInstance!.Wrapper.OpenPooledDatabase();
            try
            {
                var transaction = (SqlTransaction) await connection.BeginTransactionAsync();
                await Query(connection, transaction);
                await transaction.RollbackAsync();
                await transaction.DisposeAsync();
            }
            finally
            {
                await connection.DisposeAsync();
                sqlInstance.Wrapper.ReleasePooled(name);
            }
        }
    }

    static async Task Query(SqlConnection connection, SqlTransaction? transaction = null)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "select count(*) from dbo.WideView;";
        await command.ExecuteScalarAsync();
    }

    static async Task BuildTemplate(SqlConnection connection)
    {
        for (var index = 0; index < tableCount; index++)
        {
            await Execute(connection,
                $"""
                 create table dbo.T{index}
                 (
                     Id uniqueidentifier not null constraint PK_T{index} primary key nonclustered default newid(),
                     A nvarchar(200) null,
                     B int null
                 );
                 insert dbo.T{index} (A, B) values (N'x', 1), (N'y', 2), (N'z', 3);
                 """);
        }

        // A view that is trivial to run but costly to plan. Each branch joins across every
        // table, so the optimiser has a large search space even though the result is tiny.
        var branches = new List<string>();
        for (var index = 0; index < unionBranches; index++)
        {
            var joins = new List<string>();
            for (var table = 1; table < tableCount; table++)
            {
                joins.Add($"left join dbo.T{table} t{table} on t{table}.B = t0.B + {index % 3}");
            }

            branches.Add(
                $"""
                 select t0.Id, t0.A, t0.B
                 from dbo.T0 t0
                 {string.Join("\n", joins)}
                 where t0.B >= {index % 4}
                 """);
        }

        await Execute(connection,
            $"""
             create view dbo.WideView as
             {string.Join("\nunion all\n", branches)}
             """);
    }

    static async Task Execute(SqlConnection connection, string commandText)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = commandText;
        await command.ExecuteNonQueryAsync();
    }
}

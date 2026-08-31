/// <summary>
/// Compares the two ways a pooled database can be returned to its baseline after a test has
/// written to it: rolling the lease's transaction back, or reverting the whole database from a
/// database snapshot.
///
/// This is the closest thing LocalDB offers to what accelerated_database_recovery would have
/// given, which is undo whose cost does not scale with the size of the transaction. ADR is
/// blocked on Express (error 12128), but database snapshots do work on LocalDB, and a revert is
/// a page-level operation against the snapshot's sparse file rather than a walk backwards
/// through the log.
///
/// Both arms do the same logical thing - write Writes rows, then put the database back - so the
/// comparison is end to end. The snapshot arm includes closing and reopening the connection,
/// because a revert needs exclusive access to the database, and that reconnect is a real cost
/// of the approach rather than an artifact of the benchmark.
///
/// Result: revert loses badly. 172ms against 0.47ms at 100 rows (370x), and 307ms against 27ms
/// at 10000 rows (11x). Its cost is almost entirely fixed - dropping the connection, rebuilding
/// the log, invalidating the buffer pool - so the gap narrows as the write grows but never
/// closes at any volume a test reaches. Rolling back the lease's transaction stays the right
/// mechanism, and snapshots are recorded here as measured and rejected.
///
/// Note the I/O columns from SqlServerDiagnoser are not comparable between the two arms: BDN
/// runs vastly more invocations of the fast arm than the slow one, so those totals reflect
/// invocation count rather than per-operation cost. Only Mean and Ratio are per operation.
/// </summary>
[WarmupCount(3)]
[IterationCount(15)]
[GcServer(true)]
[SuppressMessage("Performance", "CA1822:Mark members as static")]
public class ResetStrategyBenchmarks
{
    const string InstanceName = "Benchmark";
    const string DatabaseName = "ResetStrategy";
    const string SnapshotName = "ResetStrategy_snap";
    const int Seed = 50000;

    SqlInstance? sqlInstance;
    SqlConnection? connection;
    string snapshotFile = null!;

    [Params(100, 10000)]
    public int Writes { get; set; }

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        LocalDbApi.StopAndDelete(InstanceName);
        DirectoryFinder.Delete(InstanceName);

        sqlInstance = new(name: InstanceName, buildTemplate: BuildTemplate);
        await sqlInstance.Wrapper.AwaitStart();

        var created = await sqlInstance.Wrapper.CreateDatabaseFromTemplate(DatabaseName);
        await created.DisposeAsync();

        snapshotFile = Path.Combine(sqlInstance.Wrapper.Directory, $"{SnapshotName}.ss");

        await using (var master = await OpenMaster())
        {
            await using var nameCommand = master.CreateCommand();
            nameCommand.CommandText = $"select top 1 name from [{DatabaseName}].sys.database_files where type = 0;";
            var logical = (string) (await nameCommand.ExecuteScalarAsync())!;

            await Execute(master,
                $"""
                 create database [{SnapshotName}] on
                 (
                     name = [{logical}],
                     filename = '{snapshotFile}'
                 )
                 as snapshot of [{DatabaseName}];
                 """);
        }

        connection = await sqlInstance.Wrapper.OpenExistingDatabase(DatabaseName);
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        sqlInstance?.Cleanup(ShutdownMode.KillProcess);
        sqlInstance?.Dispose();
        sqlInstance = null;
    }

    /// <summary>
    /// What BuildPooled does today: the writes happen inside the lease's transaction, and the
    /// rollback on release undoes them.
    /// </summary>
    [Benchmark(Baseline = true)]
    public async Task TransactionRollback()
    {
        var transaction = (SqlTransaction) await connection!.BeginTransactionAsync();
        try
        {
            await Execute(connection, UpdateCommand, transaction);
            await transaction.RollbackAsync();
        }
        finally
        {
            await transaction.DisposeAsync();
        }
    }

    /// <summary>
    /// The alternative: let the writes commit, then revert the whole database from the snapshot.
    /// </summary>
    [Benchmark]
    public async Task SnapshotRevert()
    {
        await Execute(connection!, UpdateCommand);

        // Revert needs exclusive access, so the lease's connection has to go first.
        await connection!.DisposeAsync();

        await using (var master = await OpenMaster())
        {
            await Execute(master, $"restore database [{DatabaseName}] from database_snapshot = '{SnapshotName}';");
        }

        connection = await sqlInstance!.Wrapper.OpenExistingDatabase(DatabaseName);
    }

    string UpdateCommand => $"update top ({Writes}) dbo.Rows set Value = N'updated';";

    async Task<SqlConnection> OpenMaster()
    {
        var master = new SqlConnection(sqlInstance!.Wrapper.MasterConnectionString);
        await master.OpenAsync();
        return master;
    }

    static async Task BuildTemplate(SqlConnection connection)
    {
        await Execute(connection,
            """
            create table dbo.Rows
            (
                Id int identity primary key,
                Value nvarchar(100) not null
            );
            """);
        await Execute(connection,
            $"""
             insert dbo.Rows (Value)
             select top ({Seed}) N'seeded'
             from sys.all_objects a
             cross join sys.all_objects b;
             """);
    }

    static async Task Execute(SqlConnection connection, string commandText, SqlTransaction? transaction = null)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = commandText;
        command.Transaction = transaction;
        await command.ExecuteNonQueryAsync();
    }
}

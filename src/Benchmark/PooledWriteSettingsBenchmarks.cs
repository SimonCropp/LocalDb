/// <summary>
/// Measures read_committed_snapshot, which a pooled lease does not need, against the
/// write-then-roll-back shape that every pooled test has.
///
/// read_committed_snapshot is set on the template for the shared database, where parallel
/// transactional tests hit the same database and would otherwise deadlock on S/X locks. A
/// pooled database is leased exclusively - one test, one connection, one transaction, enforced
/// by the lease semaphore - so nothing there can contend, and the row versioning is paid for
/// no benefit: 14 bytes appended to every modified row plus a version record per update and
/// delete.
///
/// accelerated_database_recovery was the other candidate here, since every pooled lease ends in
/// a rollback and ADR makes rollback near-instant. It cannot be measured or used: LocalDB is
/// Express edition, and enabling it fails with error 12128, "Accelerated Database Recovery
/// cannot be enabled in the Express edition of SQL Server".
///
/// The workload updates and deletes rows that were committed in the template (the case that
/// actually generates versions), inserts some more, then rolls the whole thing back.
///
/// Result: turning it off is worth ~11% wall clock, ~29% less write I/O, ~38% less read I/O and
/// ~44% less SQL Server memory, consistently across runs. It is deliberately NOT applied. The
/// setting is what lets a second connection to a leased database read while the lease holds an
/// uncommitted write, and that is public API - OpenNewConnection, NewConnectionOwnedDbContext,
/// and IDbContextFactory.CreateDbContext all open one. With RCSI on such a read returns the
/// last committed state; with it off the same read blocks on the lease's X locks until the
/// command timeout expires. This benchmark is kept as the record of that trade.
/// </summary>
[MemoryDiagnoser]
[WarmupCount(3)]
[IterationCount(20)]
[GcServer(true)]
[SuppressMessage("Performance", "CA1822:Mark members as static")]
public class PooledWriteSettingsBenchmarks
{
    const string InstanceName = "Benchmark";
    const string DatabaseName = "PooledWriteSettings";
    const int Seed = 20000;
    const int Churn = 5000;

    SqlInstance? sqlInstance;
    SqlConnection? connection;

    [Params(true, false)]
    public bool Rcsi { get; set; }

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        LocalDbApi.StopAndDelete(InstanceName);
        DirectoryFinder.Delete(InstanceName);

        sqlInstance = new(name: InstanceName, buildTemplate: BuildTemplate);
        await sqlInstance.Wrapper.AwaitStart();

        var created = await sqlInstance.Wrapper.CreateDatabaseFromTemplate(DatabaseName);
        await created.DisposeAsync();

        // Needs exclusive access, so it is applied from master before the working connection
        // is opened.
        await using (var master = new SqlConnection(sqlInstance.Wrapper.MasterConnectionString))
        {
            await master.OpenAsync();
            await Execute(master,
                $"alter database [{DatabaseName}] set read_committed_snapshot {OnOff(Rcsi)} with rollback immediate;");
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
    /// One pooled test: take the lease's transaction, write, roll back on release. Because it
    /// always rolls back, the database is identical at the start of every iteration.
    /// </summary>
    [Benchmark]
    public async Task WriteAndRollback()
    {
        var transaction = (SqlTransaction) await connection!.BeginTransactionAsync();
        try
        {
            await Execute(connection,
                $"""
                 update dbo.Rows set Value = N'updated';
                 delete top ({Churn}) from dbo.Rows;
                 insert dbo.Rows (Value)
                 select top ({Churn}) N'inserted'
                 from sys.all_objects a
                 cross join sys.all_objects b;
                 """,
                transaction);
            await transaction.RollbackAsync();
        }
        finally
        {
            await transaction.DisposeAsync();
        }
    }

    static string OnOff(bool value) => value ? "on" : "off";

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

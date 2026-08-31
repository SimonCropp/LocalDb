/// <summary>
/// Measures what a pooled lease actually spends on its rollback, as write volume grows.
///
/// Every pooled lease ends by rolling its transaction back, which is the workload
/// accelerated_database_recovery is built for: ADR undoes from a persisted version store, so
/// rollback is near-instant and independent of transaction size, where a traditional rollback
/// walks the log backwards and costs roughly one undo per log record. ADR cannot be enabled on
/// LocalDB (Express edition, error 12128), so the question is whether that matters - which
/// depends entirely on how much a rollback of a test-sized transaction actually costs.
///
/// The write is done in IterationSetup and excluded from the measurement, so the number here is
/// the rollback alone. Writes is the row count modified inside the transaction, swept across
/// three orders of magnitude to show how rollback scales.
///
/// Result: a fixed floor of ~85us plus ~0.94us per modified row, linear as expected for
/// log-walking undo. That is the whole size of the gap ADR would close, and at the volumes a
/// test actually writes it is nothing: 10 rows costs ~93us and 100 rows ~161us. It only becomes
/// material for a test modifying tens of thousands of rows, where it reaches ~9.5ms.
/// </summary>
[WarmupCount(3)]
[IterationCount(20)]
[GcServer(true)]
[SuppressMessage("Performance", "CA1822:Mark members as static")]
public class RollbackCostBenchmarks
{
    const string InstanceName = "Benchmark";
    const string DatabaseName = "RollbackCost";
    const int Seed = 50000;

    SqlInstance? sqlInstance;
    SqlConnection? connection;
    SqlTransaction? transaction;

    /// <summary>
    /// Rows modified inside the lease's transaction before it is rolled back.
    /// </summary>
    [Params(10, 100, 1000, 10000)]
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
        connection = await sqlInstance.Wrapper.OpenExistingDatabase(DatabaseName);
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        sqlInstance?.Cleanup(ShutdownMode.KillProcess);
        sqlInstance?.Dispose();
        sqlInstance = null;
    }

    [IterationSetup]
    public void IterationSetup() => Arrange().GetAwaiter().GetResult();

    [Benchmark]
    public async Task Rollback()
    {
        await transaction!.RollbackAsync();
        await transaction.DisposeAsync();
        transaction = null;
    }

    async Task Arrange()
    {
        transaction = (SqlTransaction) await connection!.BeginTransactionAsync();
        await Execute(connection,
            $"""
             update top ({Writes}) dbo.Rows set Value = N'updated';
             """,
            transaction);
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

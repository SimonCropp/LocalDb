/// <summary>
/// Confirms `alter database template set mixed_page_allocation on` in
/// <c>SqlBuilder.GetCreateTemplateCommand</c>.
///
/// Test schemas are typically many small tables. With the SQL Server default (off) each index
/// reserves a dedicated 64KB uniform extent, so a seeded template of small tables is mostly
/// empty extents — and that bloat is copied to every per-test database. Setting mixed_page on
/// before the schema is built packs small objects into shared extents, keeping the template
/// (and every clone of it) far smaller.
///
/// Builds a many-small-temporal-table template with the setting on vs off, then clones
/// databases from it. The on variant copies a much smaller file to each clone: compare the
/// Mean (clone time) of the on/off rows and the template.mdf size logged during setup. (The
/// clone copy is an OS file copy, so it does not show in the SQL I/O diagnoser columns.)
/// </summary>
[MemoryDiagnoser]
[WarmupCount(5)]
[IterationCount(25)]
[GcServer(true)]
[SuppressMessage("Performance", "CA1822:Mark members as static")]
public class MixedPageAllocationBenchmarks
{
    const string InstanceName = "Benchmark";
    SqlInstance? sqlInstance;
    int databaseCounter;

    [Params(10, 40, 80)]
    public int TableCount { get; set; }

    [Params(false, true)]
    public bool MixedPageAllocation { get; set; }

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        LocalDbApi.StopAndDelete(InstanceName);
        DirectoryFinder.Delete(InstanceName);

        sqlInstance = new(name: InstanceName, buildTemplate: BuildManySmallTables);
        await sqlInstance.Wrapper.AwaitStart();

        var template = Path.Combine(sqlInstance.Wrapper.Directory, "template.mdf");
        var sizeMb = new FileInfo(template).Length / (1024.0 * 1024.0);
        Console.WriteLine($"// mixed_page_allocation={MixedPageAllocation}: template.mdf = {sizeMb:F1} MB ({TableCount} tables)");
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        sqlInstance?.Cleanup(ShutdownMode.KillProcess);
        sqlInstance?.Dispose();
        sqlInstance = null;
    }

    [Benchmark]
    public async Task CloneDatabase()
    {
        await using var database = await sqlInstance!.Build($"Clone{Interlocked.Increment(ref databaseCounter)}");
    }

    async Task BuildManySmallTables(SqlConnection connection)
    {
        // SqlBuilder sets mixed_page_allocation on at template creation. Flip it back off here to
        // measure the pre-fix baseline; it must be set before the schema is built so allocations
        // use (or don't use) mixed extents from the start.
        await Execute(connection,
            $"alter database template set mixed_page_allocation {(MixedPageAllocation ? "on" : "off")};");

        for (var index = 0; index < TableCount; index++)
        {
            await Execute(connection,
                $"""
                 create table dbo.T{index}
                 (
                     Id uniqueidentifier not null constraint PK_T{index} primary key nonclustered default newid(),
                     A nvarchar(200) null,
                     B nvarchar(200) null,
                     C int null,
                     D uniqueidentifier null,
                     ValidFrom datetime2 generated always as row start hidden not null,
                     ValidTo datetime2 generated always as row end hidden not null,
                     period for system_time (ValidFrom, ValidTo)
                 )
                 with (system_versioning = on (history_table = dbo._T{index}_History));
                 create index IX_T{index}_C on dbo.T{index} (C);
                 create index IX_T{index}_D on dbo.T{index} (D);
                 insert dbo.T{index} (A, B, C) values (N'x', N'y', 1), (N'p', N'q', 2), (N'm', N'n', 3);
                 """);
        }
    }

    static async Task Execute(SqlConnection connection, string commandText)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = commandText;
        await command.ExecuteNonQueryAsync();
    }
}

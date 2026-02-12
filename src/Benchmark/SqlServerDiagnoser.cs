public class SqlServerDiagnoser : IDiagnoser
{
    const string diagnoserId = nameof(SqlServerDiagnoser);

    readonly List<SqlServerMetrics> metrics = [];
    long ioReadBytesBefore;
    long ioWriteBytesBefore;

    public IEnumerable<string> Ids => [diagnoserId];
    public IEnumerable<IExporter> Exporters => [];
    public IEnumerable<IAnalyser> Analysers => [];

    public RunMode GetRunMode(BenchmarkCase benchmarkCase) => RunMode.NoOverhead;

#pragma warning disable CA1822
    public bool RequiresBlockingAcknowledgments(BenchmarkCase benchmarkCase) => false;
#pragma warning restore CA1822

    public void Handle(HostSignal signal, DiagnoserActionParameters parameters)
    {
        var connectionString = "Data Source=(LocalDb)\\Benchmark;Database=master;Pooling=true";

        switch (signal)
        {
            case HostSignal.BeforeActualRun:
                    (ioReadBytesBefore, ioWriteBytesBefore) = GetFileIoStats(connectionString);
                break;

            case HostSignal.AfterActualRun:
                    var (ioReadBytesAfter, ioWriteBytesAfter) = GetFileIoStats(connectionString);
                    var memoryMb = GetSqlServerMemory(connectionString);

                    metrics.Add(
                        new()
                    {
                        BenchmarkName = parameters.BenchmarkCase.Descriptor.WorkloadMethod.Name,
                        IoReadMB = (ioReadBytesAfter - ioReadBytesBefore) / (1024.0 * 1024.0),
                        IoWriteMB = (ioWriteBytesAfter - ioWriteBytesBefore) / (1024.0 * 1024.0),
                        MemoryMB = memoryMb
                    });
                break;
        }
    }

    static (long readBytes, long writeBytes) GetFileIoStats(string connectionString)
    {
        using var connection = new SqlConnection(connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                SUM(num_of_bytes_read) as read_bytes,
                SUM(num_of_bytes_written) as write_bytes
            FROM sys.dm_io_virtual_file_stats(NULL, NULL)
            """;
        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return (
                reader.IsDBNull(0) ? 0 : reader.GetInt64(0),
                reader.IsDBNull(1) ? 0 : reader.GetInt64(1)
            );
        }
        return (0, 0);
    }

    static double GetSqlServerMemory(string connectionString)
    {
        using var connection = new SqlConnection(connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        // Try sys.dm_os_process_memory which LocalDB supports
        command.CommandText = """
            SELECT physical_memory_in_use_kb / 1024.0 as memory_mb
            FROM sys.dm_os_process_memory
            """;
        var result = command.ExecuteScalar();
        return result switch
        {
            double d => d,
            decimal dec => (double)dec,
            long l => l,
            int i => i,
            _ => 0
        };
    }

    public IEnumerable<Metric> ProcessResults(DiagnoserResults results) => [];

    public void DisplayResults(ILogger logger)
    {
        if (metrics.Count == 0)
        {
            logger.WriteLine();
            logger.WriteLineHeader("// * SQL Server Metrics: No data captured *");
            return;
        }

        logger.WriteLine();
        logger.WriteLineHeader("// * SQL Server (LocalDB) Metrics *");
        logger.WriteLine();
        logger.WriteLine("| Method              | I/O Read (MB) | I/O Write (MB) | Memory (MB) |");
        logger.WriteLine("|---------------------|---------------:|---------------:|------------:|");

        foreach (var group in metrics.GroupBy(_ => _.BenchmarkName))
        {
            var avg = new SqlServerMetrics
            {
                BenchmarkName = group.Key,
                IoReadMB = group.Average(_ => _.IoReadMB),
                IoWriteMB = group.Average(_ => _.IoWriteMB),
                MemoryMB = group.Average(_ => _.MemoryMB)
            };

            logger.WriteLine($"| {avg.BenchmarkName,-19} | {avg.IoReadMB,14:F2} | {avg.IoWriteMB,14:F2} | {avg.MemoryMB,11:F2} |");
        }

        logger.WriteLine();
    }

    public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters) => [];

    record SqlServerMetrics
    {
        public required string BenchmarkName { get; init; }
        public required double IoReadMB { get; init; }
        public required double IoWriteMB { get; init; }
        public required double MemoryMB { get; init; }
    }
}

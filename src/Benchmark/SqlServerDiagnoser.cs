public class SqlServerDiagnoser : IDiagnoser
{
    const string diagnoserId = nameof(SqlServerDiagnoser);
    // The diagnoser runs in the BDN host process, which is a different process from the benchmark
    // child process in the default OutOfProcess toolchain. Static state set in the child is invisible
    // here, so the connection string must be self-contained. The LocalDB named instance is per-user
    // so any process under the same user can connect once the benchmark has started it.
    // AiCliDetector runs in both host and child (env inherited), so both compute the same prefix.
    static readonly string connectionString =
        $"Data Source=(LocalDb)\\{(AiCliDetector.Detected ? "chatbot_Benchmark" : "Benchmark")};Initial Catalog=master;Pooling=false";

    readonly List<SqlServerMetrics> metrics = [];
    readonly List<string> failures = [];
    long ioReadBytesBefore;
    long ioWriteBytesBefore;
    bool beforeCaptured;

    public IEnumerable<string> Ids => [diagnoserId];
    public IEnumerable<IExporter> Exporters => [];
    public IEnumerable<IAnalyser> Analysers => [];

    public RunMode GetRunMode(BenchmarkCase benchmarkCase) => RunMode.NoOverhead;

#pragma warning disable CA1822
    public bool RequiresBlockingAcknowledgments(BenchmarkCase benchmarkCase) => false;
#pragma warning restore CA1822

    public void Handle(HostSignal signal, DiagnoserActionParameters parameters)
    {
        switch (signal)
        {
            case HostSignal.BeforeActualRun:
                beforeCaptured = TryGetFileIoStats(connectionString, out ioReadBytesBefore, out ioWriteBytesBefore);
                break;

            case HostSignal.AfterActualRun:
                if (!beforeCaptured)
                {
                    return;
                }

                if (!TryGetFileIoStats(connectionString, out var ioReadBytesAfter, out var ioWriteBytesAfter))
                {
                    return;
                }

                var memoryMb = TryGetSqlServerMemory(connectionString);

                metrics.Add(
                    new()
                    {
                        BenchmarkName = Describe(parameters.BenchmarkCase),
                        IoReadMB = (ioReadBytesAfter - ioReadBytesBefore) / (1024.0 * 1024.0),
                        IoWriteMB = (ioWriteBytesAfter - ioWriteBytesBefore) / (1024.0 * 1024.0),
                        MemoryMB = memoryMb
                    });
                break;
        }
    }

    bool TryGetFileIoStats(string connectionString, out long readBytes, out long writeBytes)
    {
        readBytes = 0;
        writeBytes = 0;
        try
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
                readBytes = reader.IsDBNull(0) ? 0 : reader.GetInt64(0);
                writeBytes = reader.IsDBNull(1) ? 0 : reader.GetInt64(1);
            }

            return true;
        }
        catch (Exception exception)
        {
            failures.Add($"GetFileIoStats: {exception.GetType().Name}: {exception.Message}");
            return false;
        }
    }

    double TryGetSqlServerMemory(string connectionString)
    {
        try
        {
            using var connection = new SqlConnection(connectionString);
            connection.Open();
            using var command = connection.CreateCommand();
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
        catch (Exception exception)
        {
            failures.Add($"GetSqlServerMemory: {exception.GetType().Name}: {exception.Message}");
            return 0;
        }
    }

    // Include parameter values so parameterized benchmarks (e.g. mixed_page_allocation on vs off)
    // are reported as separate rows instead of being averaged together under the method name.
    static string Describe(BenchmarkCase benchmarkCase)
    {
        var method = benchmarkCase.Descriptor.WorkloadMethod.Name;
        var paramInfo = benchmarkCase.Parameters.DisplayInfo;
        return paramInfo.Length == 0 ? method : $"{method} {paramInfo}";
    }

    public IEnumerable<Metric> ProcessResults(DiagnoserResults results) => [];

    public void DisplayResults(ILogger logger)
    {
        if (failures.Count > 0)
        {
            logger.WriteLine();
            logger.WriteLineHeader("// * SQL Server Diagnoser failures *");
            foreach (var failure in failures.Distinct())
            {
                logger.WriteLineError($"// {failure}");
            }
        }

        if (metrics.Count == 0)
        {
            logger.WriteLine();
            logger.WriteLineHeader("// * SQL Server Metrics: No data captured *");
            return;
        }

        logger.WriteLine();
        logger.WriteLineHeader("// * SQL Server (LocalDB) Metrics *");
        logger.WriteLine();
        logger.WriteLine($"| {"Method",-50} | I/O Read (MB) | I/O Write (MB) | Memory (MB) |");
        logger.WriteLine($"|{new string('-', 52)}|---------------:|---------------:|------------:|");

        foreach (var group in metrics.GroupBy(_ => _.BenchmarkName))
        {
            var avg = new SqlServerMetrics
            {
                BenchmarkName = group.Key,
                IoReadMB = group.Average(_ => _.IoReadMB),
                IoWriteMB = group.Average(_ => _.IoWriteMB),
                MemoryMB = group.Average(_ => _.MemoryMB)
            };

            logger.WriteLine($"| {avg.BenchmarkName,-50} | {avg.IoReadMB,14:F2} | {avg.IoWriteMB,14:F2} | {avg.MemoryMB,11:F2} |");
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

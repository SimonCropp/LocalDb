using BenchmarkDotNet.Configs;

var config = DefaultConfig.Instance
    .AddDiagnoser(new SqlServerDiagnoser());

BenchmarkRunner.Run<LocalDbBenchmarks>(config);

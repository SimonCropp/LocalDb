using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

var config = DefaultConfig.Instance
    .AddDiagnoser(new SqlServerDiagnoser());

BenchmarkRunner.Run<LocalDbBenchmarks>(config);

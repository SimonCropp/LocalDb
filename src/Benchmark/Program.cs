using BenchmarkDotNet.Configs;

var config = DefaultConfig.Instance
    .AddDiagnoser(new SqlServerDiagnoser());

BenchmarkSwitcher.FromTypes(
[
    typeof(LocalDbBenchmarks),
    typeof(ColdStartBenchmarks),
    typeof(StoppedInstanceBenchmarks),
    typeof(WarmStartBenchmarks),
    typeof(TemplateRebuildBenchmarks)
]).Run(args, config);

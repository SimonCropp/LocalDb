using BenchmarkDotNet.Configs;

var config = DefaultConfig.Instance
    .AddDiagnoser(new SqlServerDiagnoser());

BenchmarkSwitcher.FromTypes(
[
    typeof(LocalDbBenchmarks),
    typeof(ColdStartBenchmarks),
    typeof(StoppedInstanceBenchmarks),
    typeof(WarmStartBenchmarks),
    typeof(TemplateRebuildBenchmarks),
    typeof(MixedPageAllocationBenchmarks),
    typeof(MixedPageContentionBenchmarks),
    typeof(DelayedDurabilityBenchmarks),
    typeof(AutoCloseBenchmarks)
]).Run(args, config);

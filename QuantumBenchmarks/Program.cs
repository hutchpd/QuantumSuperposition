using BenchmarkDotNet.Running;
using QuantumBenchmarks.Benchmarks;

BenchmarkRunner.Run<MultiQubitGateBenchmarks>();
BenchmarkRunner.Run<SetFromTensorProductBenchmarks>();
BenchmarkRunner.Run<GateQueueOptimizationBenchmarks>();

using BenchmarkDotNet.Attributes;
using QuantumSuperposition.Systems;
using QuantumSuperposition.QuantumSoup;
using System.Numerics;

namespace QuantumBenchmarks.Benchmarks;

[MemoryDiagnoser]
[RankColumn]
public class SetFromTensorProductBenchmarks
{
    [Params(6, 12)] public int QubitCount;
    private QuantumSystem _system = null!;

    [GlobalSetup]
    public void Setup()
    {
        _system = new QuantumSystem();
    }

    [Benchmark]
    public void BuildTensorProduct()
    {
        var qubits = Enumerable.Range(0, QubitCount)
            .Select(i => new QuBit<int>(_system, new[] { i }).WithWeights(new Dictionary<int, Complex> { { 0, 1.0 }, { 1, 1.0 } }, true))
            .ToArray();
        _system.SetFromTensorProduct(false, qubits);
    }
}

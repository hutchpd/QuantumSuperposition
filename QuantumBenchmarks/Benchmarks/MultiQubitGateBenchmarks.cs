using BenchmarkDotNet.Attributes;
using QuantumSuperposition.Systems;
using QuantumSuperposition.QuantumSoup;
using QuantumSuperposition.Utilities;
using System.Numerics;

namespace QuantumBenchmarks.Benchmarks;

[MemoryDiagnoser]
[RankColumn]
public class MultiQubitGateBenchmarks
{
    [Params(6, 10)] public int QubitCount;
    private QuantumSystem _system = null!;
    private Complex[,] _hadamard;

    [GlobalSetup]
    public void Setup()
    {
        _system = new QuantumSystem();
        // Initialise qubits in |0> + |1> superposition via tensor product
        var qubits = Enumerable.Range(0, QubitCount)
            .Select(i => new QuBit<int>(_system, new[] { i }).WithWeights(new Dictionary<int, Complex> { { 0, 1.0 }, { 1, 1.0 } }, true))
            .ToArray();
        _system.SetFromTensorProduct(false, qubits);
        _hadamard = QuantumGates.Hadamard; // 2x2
    }

    [Benchmark]
    public void ApplyAllHadamards()
    {
        for (int i = 0; i < QubitCount; i++)
        {
            _system.ApplySingleQubitGate(i, _hadamard, "H");
        }
        _system.ProcessGateQueue();
    }
}

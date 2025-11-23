using BenchmarkDotNet.Attributes;
using QuantumSuperposition.Systems;
using QuantumSuperposition.QuantumSoup;
using QuantumSuperposition.Utilities;
using System.Numerics;

namespace QuantumBenchmarks.Benchmarks;

[MemoryDiagnoser]
[RankColumn]
public class GateQueueOptimizationBenchmarks
{
    private QuantumSystem _system = null!;
    private Complex[,] _hadamard;

    [Params(8, 16)] public int QubitCount;

    [GlobalSetup]
    public void Setup()
    {
        _system = new QuantumSystem();
        var qubits = Enumerable.Range(0, QubitCount)
            .Select(i => new QuBit<int>(_system, new[] { i }).WithWeights(new Dictionary<int, Complex> { { 0, 1.0 }, { 1, 1.0 } }, true))
            .ToArray();
        _system.SetFromTensorProduct(false, qubits);
        _hadamard = QuantumGates.Hadamard;
    }

    [Benchmark]
    public void QueueAndProcessGates()
    {
        // Add a pattern of gates including cancellations (H then H) to test optimisation.
        for (int round = 0; round < 5; round++)
        {
            for (int i = 0; i < QubitCount; i++)
            {
                _system.ApplySingleQubitGate(i, _hadamard, "H");
                _system.ApplySingleQubitGate(i, _hadamard, "H"); // should cancel in optimisation
            }
        }
        _system.ProcessGateQueue();
    }
}

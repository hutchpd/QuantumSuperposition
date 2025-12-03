using System;
using System.IO;
using System.Linq;
using System.Numerics;
using QuantumSuperposition.Core;
using QuantumSuperposition.Systems;
using QuantumSuperposition.Utilities;

internal static class Program
{
    public static void Main()
    {
        // Section 8.3: Grover search over a 2-qubit database
        // We produce console output and CSV artifacts for plotting and measurement statistics.
        string outDir = Path.Combine("Artifacts", "Grover", "2Qubits-OneTarget");
        Directory.CreateDirectory(outDir);

        // Build a QuantumSystem with 2 qubits initialised to uniform superposition via H ⊗ H
        QuantumSystem system = new QuantumSystem();
        // Start in |00⟩ with explicit amplitudes
        var initDict = new System.Collections.Generic.Dictionary<int[], Complex>(new QuantumSuperposition.Utilities.IntArrayComparer())
        {
            { new[] {0,0}, Complex.One }
        };
        system.SetAmplitudes(initDict);

        // Apply H on both qubits to create uniform superposition
        system.ApplyMultiQubitGate(new[] { 0 }, QuantumGates.Hadamard.Matrix, "H");
        system.ApplyMultiQubitGate(new[] { 1 }, QuantumGates.Hadamard.Matrix, "H");
        // no queue processing needed (multi-qubit API applies immediately)

        // Initial uniform superposition amplitudes
        WriteAmpsCsv(Path.Combine(outDir, "initial_uniform.csv"), system);
        Console.WriteLine("=== Initial uniform (H ⊗ H) ===\n" + system.GetAmplitudePhaseDebugView());

        // Define oracle: mark target |10⟩ (MSB-first convention used by helpers/tests)
        static bool Oracle(int[] bits) => bits.Length == 2 && bits[0] == 1 && bits[1] == 0;

        // Perform one Grover iteration manually to capture intermediate amplitudes
        ApplyOracle(system, new[] { 0, 1 }, Oracle);
        ApplyDiffusion(system, new[] { 0, 1 });

        // After one iteration amplitudes
        WriteAmpsCsv(Path.Combine(outDir, "after_one_iteration.csv"), system);
        Console.WriteLine("=== After one Grover iteration ===\n" + system.GetAmplitudePhaseDebugView());

        // Final measurement statistics over many runs (rebuild system each run to ensure identical start)
        int runs = 5000;
        var counts = new int[4]; // indices 0..3 for |00>,|01>,|10>,|11>
        Random rng = new Random(42);
        for (int i = 0; i < runs; i++)
        {
            QuantumSystem s = new QuantumSystem();
            var startDict = new System.Collections.Generic.Dictionary<int[], Complex>(new QuantumSuperposition.Utilities.IntArrayComparer())
            {
                { new[] {0,0}, Complex.One }
            };
            s.SetAmplitudes(startDict);
            s.ApplyMultiQubitGate(new[] { 0 }, QuantumGates.Hadamard.Matrix, "H");
            s.ApplyMultiQubitGate(new[] { 1 }, QuantumGates.Hadamard.Matrix, "H");
            // no queue processing
            ApplyOracle(s, new[] { 0, 1 }, Oracle);
            ApplyDiffusion(s, new[] { 0, 1 });
            int[] observed = s.PartialObserve(new[] { 0, 1 }, rng);
            int idx = (observed[0] << 1) | observed[1];
            counts[idx]++;
        }

        // Write measurement histogram CSV
        using (var sw = new StreamWriter(Path.Combine(outDir, "measurement_histogram.csv")))
        {
            sw.WriteLine("basis,state,count,frequency");
            for (int i = 0; i < 4; i++)
            {
                int[] bits = QuantumAlgorithms.IndexToBits(i, 2);
                double freq = counts[i] / (double)runs;
                sw.WriteLine($"{i},|{bits[0]}{bits[1]}>,{counts[i]},{freq:F6}");
            }
        }

        Console.WriteLine("=== Measurement histogram (" + runs + ") ===");
        for (int i = 0; i < 4; i++)
        {
            int[] bits = QuantumAlgorithms.IndexToBits(i, 2);
            Console.WriteLine($"|{bits[0]}{bits[1]}>: {counts[i]} ({counts[i] / (double)runs:P2})");
        }

        Console.WriteLine("Artifacts written to: " + outDir);
    }

    // Write current system amplitudes to CSV for plotting
    private static void WriteAmpsCsv(string path, QuantumSystem system)
    {
        using var sw = new StreamWriter(path);
        sw.WriteLine("bits,index,real,imag,prob");
        foreach (var kv in system.Amplitudes)
        {
            int[] bits = kv.Key;
            int idx = QuantumAlgorithms.BitsToIndex(bits);
            Complex a = kv.Value;
            double p = a.Magnitude * a.Magnitude;
            sw.WriteLine($"{string.Join("", bits)},{idx},{a.Real},{a.Imaginary},{p}");
        }
    }

    // Apply oracle: phase flip on target basis
    private static void ApplyOracle(QuantumSystem system, int[] qubits, Func<int[], bool> oracle)
    {
        int n = qubits.Length;
        int dim = 1 << n;
        Complex[,] m = new Complex[dim, dim];
        for (int i = 0; i < dim; i++)
        {
            int[] basis = QuantumAlgorithms.IndexToBits(i, n);
            m[i, i] = oracle(basis) ? -Complex.One : Complex.One;
        }
        system.ApplyMultiQubitGate(qubits, m, "Oracle");
    }

    // Apply diffusion operator (H all, X all, MCZ on |00>, X all, H all)
    private static void ApplyDiffusion(QuantumSystem system, int[] qubits)
    {
        foreach (int q in qubits) system.ApplyMultiQubitGate(new[] { q }, QuantumGates.Hadamard.Matrix, "H");
        foreach (int q in qubits) system.ApplyMultiQubitGate(new[] { q }, QuantumGates.PauliX.Matrix, "X");
        int n = qubits.Length; int dim = 1 << n;
        Complex[,] mcz = new Complex[dim, dim];
        for (int i = 0; i < dim; i++) mcz[i, i] = (i == 0) ? -Complex.One : Complex.One;
        system.ApplyMultiQubitGate(qubits, mcz, "MCZ");
        foreach (int q in qubits) system.ApplyMultiQubitGate(new[] { q }, QuantumGates.PauliX.Matrix, "X");
        foreach (int q in qubits) system.ApplyMultiQubitGate(new[] { q }, QuantumGates.Hadamard.Matrix, "H");
        // no queue processing
    }
}

using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Hosting;
using PositronicVariables.Attributes;
using PositronicVariables.DependencyInjection;
using PositronicVariables.Neural;
using PositronicVariables.Runtime;
using PositronicVariables.Transactions;
using PositronicVariables.Variables;
using QuantumSuperposition.QuantumSoup;

internal static class Program
{
    // Section 8.4: Temporal convergence and paradox handling (PositronicVariables)
    // We print summaries and emit CSV artifacts for the paper.
    [DontPanic]
    internal static void Main()
    {
        // Ensure ambient runtime exists for PositronicVariables
        if (!PositronicVariables.Runtime.PositronicAmbient.IsInitialized)
        {
            IHostBuilder hb = Host.CreateDefaultBuilder().ConfigureServices(s => s.AddPositronicRuntime());
            PositronicVariables.Runtime.PositronicAmbient.InitialiseWith(hb);
        }

        string outDir = Path.Combine("Artifacts", "Positronic", "TemporalConvergence");
        Directory.CreateDirectory(outDir);

        var rt = PositronicVariables.Runtime.PositronicAmbient.Current;

        // Demo 1: Antival paradox — shows convergence to a two-state superposition any(-1, 1)
        SetupAntivalParadox(rt);

        // Demo 2: Neural nodules feedback loop (toy consensus)
        SetupNeuralConsensus(rt);

        // Write artifacts only once convergence has been achieved, on forward entropy
        if (rt.Entropy > 0 && rt.Converged)
        {
            EmitAntivalArtifacts(outDir);
            EmitNeuralArtifacts(outDir);
            Console.WriteLine("Artifacts written to: " + outDir);
        }
    }

    // --- Antival ---
    private static PositronicVariable<int> _antival;
    private static void SetupAntivalParadox(IPositronicRuntime rt)
    {
        _antival = PositronicVariable<int>.GetOrCreate("antival", -1, rt);
        // Define paradox: a = -a
        var val = -1 * _antival;
        _antival.Required = val;
    }

    private static void EmitAntivalArtifacts(string outDir)
    {
        if (_antival == null) return;
        int[] final = _antival.ToValues().OrderBy(x => x).ToArray();
        Console.WriteLine($"[Antival] final states: {(final.Length == 1 ? final[0].ToString() : $"any({string.Join(", ", final)})")}");
        using var sw = new StreamWriter(Path.Combine(outDir, "antival_paradox.csv"));
        sw.WriteLine("variable,states");
        sw.WriteLine($"antival,{string.Join(" ", final)}");
    }

    // --- Neural consensus ---
    private static PositronicVariable<int> _n1, _n2, _n3;
    private static NeuralNodule<int> _node1, _node2, _node3;
    private static int _iterations;

    private static void SetupNeuralConsensus(IPositronicRuntime rt)
    {
        STMTelemetry.Reset();

        // Variables participating in a tiny consensus-like feedback: three nodes Agree/Disagree encoded as {-1, 1}
        _n1 = PositronicVariable<int>.GetOrCreate("n1", -1, rt);
        _n2 = PositronicVariable<int>.GetOrCreate("n2", 1, rt);
        _n3 = PositronicVariable<int>.GetOrCreate("n3", -1, rt);

        // Neural nodules: each node tries to align with the majority of inputs with a small bias
        // Activation function returns a superposition of current majority and a +1 branch to allow stabilisation
        _node1 = new(vals =>
        {
            int sum = vals.Sum();
            if (sum == 0)
            {
                return new QuBit<int>(new[] { -1, 1 }).Any();
            }
            int majority = Math.Sign(sum);
            return new QuBit<int>(new[] { majority, -majority }).Any();
        }, rt);
        _node2 = new(vals =>
        {
            int sum = vals.Sum();
            if (sum == 0) return new QuBit<int>(new[] { -1, 1 }).Any();
            int majority = Math.Sign(sum);
            return new QuBit<int>(new[] { majority }).Any();
        }, rt);
        _node3 = new(vals =>
        {
            int sum = vals.Sum();
            if (sum == 0) return new QuBit<int>(new[] { -1, 1 }).Any();
            int majority = Math.Sign(sum);
            return new QuBit<int>(new[] { majority }).Any();
        }, rt);

        // Wire inputs. Each node sees others to encourage consensus.
        _node1.Inputs.Add(_n2); _node1.Inputs.Add(_n3);
        _node2.Inputs.Add(_n1); _node2.Inputs.Add(_n3);
        _node3.Inputs.Add(_n1); _node3.Inputs.Add(_n2);

        // On each forward iteration, fire nodules and bind outputs back via Required
        if (rt.Entropy > 0)
        {
            _iterations++;
            _node1.Fire(); _n1.Required = _node1.Output.State;
            _node2.Fire(); _n2.Required = _node2.Output.State;
            _node3.Fire(); _n3.Required = _node3.Output.State;
        }
    }

    private static void EmitNeuralArtifacts(string outDir)
    {
        if (_n1 == null || _n2 == null || _n3 == null) return;
        int[] s1 = _n1.ToValues().OrderBy(x => x).ToArray();
        int[] s2 = _n2.ToValues().OrderBy(x => x).ToArray();
        int[] s3 = _n3.ToValues().OrderBy(x => x).ToArray();

        Console.WriteLine($"[Consensus] iterations={_iterations}");
        Console.WriteLine($"[Consensus] n1={FmtStates(s1)} n2={FmtStates(s2)} n3={FmtStates(s3)}");

        var report = STMTelemetry.GetReport();
        Console.WriteLine("[Consensus] telemetry:\n" + report);

        using (var sw = new StreamWriter(Path.Combine(outDir, "consensus_summary.csv")))
        {
            sw.WriteLine("metric,value");
            sw.WriteLine($"iterations,{_iterations}");
        }
        using (var sw = new StreamWriter(Path.Combine(outDir, "consensus_final_states.csv")))
        {
            sw.WriteLine("variable,states");
            sw.WriteLine($"n1,{string.Join(" ", s1)}");
            sw.WriteLine($"n2,{string.Join(" ", s2)}");
            sw.WriteLine($"n3,{string.Join(" ", s3)}");
        }
        File.WriteAllText(Path.Combine(outDir, "consensus_telemetry.txt"), report);
    }

    private static string FmtStates(int[] s) => s.Length == 1 ? s[0].ToString() : $"any({string.Join(", ", s)})";
}

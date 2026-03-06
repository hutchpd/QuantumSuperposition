using QuantumSuperposition.Core;
using QuantumSuperposition.NoiseProperties;
using QuantumSuperposition.QuantumSoup;
using System.Numerics;

namespace QuantumSuperposition.Systems
{
    /// <summary>
    /// Decorator/layer around <see cref="QuantumSystem"/> that injects probabilistic errors
    /// after ideal gate applications.
    /// </summary>
    public sealed class NoisyQuantumSystem
    {
        public QuantumSystem InnerSystem { get; }
        public NoiseModel NoiseModel { get; }

        public NoisyQuantumSystem(NoiseModel noiseModel, QuantumSystem? innerSystem = null)
        {
            NoiseModel = noiseModel ?? throw new ArgumentNullException(nameof(noiseModel));
            InnerSystem = innerSystem ?? new QuantumSystem();
        }

        public IReadOnlyDictionary<int[], Complex> Amplitudes => InnerSystem.Amplitudes;

        public void ProcessGateQueue() => InnerSystem.ProcessGateQueue();

        public void Apply(QuantumGate gate, IQuantumReference target, string? gateName = null, Random? rng = null)
        {
            if (target is null) throw new ArgumentNullException(nameof(target));
            int[] indices = target.GetQubitIndices();
            Apply(gate, indices, gateName, rng);
        }

        public void Apply(QuantumGate gate, int qubitIndex, string? gateName = null, Random? rng = null)
        {
            Apply(gate, new[] { qubitIndex }, gateName, rng);
        }

        public void Apply(QuantumGate gate, int qubitA, int qubitB, string? gateName = null, Random? rng = null)
        {
            Apply(gate, new[] { qubitA, qubitB }, gateName, rng);
        }

        public void Apply(QuantumGate gate, int[] targetQubits, string? gateName = null, Random? rng = null)
        {
            if (gate is null) throw new ArgumentNullException(nameof(gate));
            if (targetQubits is null) throw new ArgumentNullException(nameof(targetQubits));

            rng ??= Random.Shared;
            gateName ??= TryInferGateName(gate) ?? "Gate";

            int dim0 = gate.Matrix.GetLength(0);
            int dim1 = gate.Matrix.GetLength(1);

            if (targetQubits.Length == 1 && dim0 == 2 && dim1 == 2)
            {
                InnerSystem.ApplySingleQubitGate(targetQubits[0], gate.Matrix, gateName);
                MaybeInjectSingleQubitError(targetQubits[0], rng);
                return;
            }

            if (targetQubits.Length == 2 && dim0 == 4 && dim1 == 4)
            {
                InnerSystem.ApplyTwoQubitGate(targetQubits[0], targetQubits[1], gate.Matrix, gateName);
                MaybeInjectTwoQubitError(targetQubits[0], targetQubits[1], rng);
                return;
            }

            // For now: keep multi-qubit behaviour unchanged (processed immediately by QuantumSystem).
            // Noise injection for this path can be added later once we decide on ordering semantics.
            InnerSystem.ApplyMultiQubitGate(targetQubits, gate.Matrix, gateName);
        }

        public int[] ObserveGlobal(int[] qubitIndices, Random? rng = null)
        {
            rng ??= Random.Shared;
            int[] ideal = InnerSystem.ObserveGlobal(qubitIndices, rng);
            return ApplyReadoutNoise(ideal, rng);
        }

        public int[] PartialObserve(int[] measuredIndices, Random? rng = null)
        {
            rng ??= Random.Shared;
            int[] ideal = InnerSystem.PartialObserve(measuredIndices, rng);
            return ApplyReadoutNoise(ideal, rng);
        }

        private int[] ApplyReadoutNoise(int[] idealBits, Random rng)
        {
            if (idealBits.Length == 0) return idealBits;

            ReadoutErrorMatrix m = NoiseModel.ReadoutErrorMatrix;
            if (m.Equals(ReadoutErrorMatrix.Identity)) return idealBits;

            int[] noisy = (int[])idealBits.Clone();
            for (int i = 0; i < noisy.Length; i++)
            {
                int actual = noisy[i];
                if (actual != 0 && actual != 1) continue;

                double flipProb = actual == 0 ? m.P01 : m.P10;
                if (flipProb <= 0.0) continue;

                if (rng.NextDouble() < flipProb)
                {
                    noisy[i] = 1 - actual;
                }
            }

            return noisy;
        }

        private void MaybeInjectSingleQubitError(int qubitIndex, Random rng)
        {
            double p = NoiseModel.SingleQubitErrorRate;
            if (p <= 0.0) return;
            if (rng.NextDouble() >= p) return;

            bool useX = rng.Next(2) == 0;
            QuantumGate err = useX ? QuantumGates.PauliX : QuantumGates.PauliZ;
            string name = useX ? "X" : "Z";

            InnerSystem.ApplySingleQubitGate(qubitIndex, err.Matrix, name);
        }

        private void MaybeInjectTwoQubitError(int qubitA, int qubitB, Random rng)
        {
            double p = NoiseModel.TwoQubitErrorRate;
            if (p <= 0.0) return;
            if (rng.NextDouble() >= p) return;

            int target = rng.Next(2) == 0 ? qubitA : qubitB;
            bool useX = rng.Next(2) == 0;
            QuantumGate err = useX ? QuantumGates.PauliX : QuantumGates.PauliZ;
            string name = useX ? "X" : "Z";

            InnerSystem.ApplySingleQubitGate(target, err.Matrix, name);
        }

        private static string? TryInferGateName(QuantumGate gate)
        {
            if (IsSameMatrix(gate.Matrix, QuantumGates.Hadamard.Matrix)) return "H";
            if (IsSameMatrix(gate.Matrix, QuantumGates.PauliX.Matrix)) return "X";
            if (IsSameMatrix(gate.Matrix, QuantumGates.PauliY.Matrix)) return "Y";
            if (IsSameMatrix(gate.Matrix, QuantumGates.PauliZ.Matrix)) return "Z";
            if (IsSameMatrix(gate.Matrix, QuantumGates.CNOT.Matrix)) return "CNOT";
            if (IsSameMatrix(gate.Matrix, QuantumGates.SWAP.Matrix)) return "SWAP";
            if (IsSameMatrix(gate.Matrix, QuantumGates.Identity.Matrix)) return "I";
            return null;
        }

        private static bool IsSameMatrix(Complex[,] a, Complex[,] b)
        {
            if (a.GetLength(0) != b.GetLength(0) || a.GetLength(1) != b.GetLength(1)) return false;

            for (int i = 0; i < a.GetLength(0); i++)
            {
                for (int j = 0; j < a.GetLength(1); j++)
                {
                    if (a[i, j] != b[i, j]) return false;
                }
            }

            return true;
        }
    }
}

using QuantumSuperposition.Systems;
using System.Numerics;

namespace QuantumSuperposition.Core
{

    public static class QuantumAlgorithms
    {
        /// <summary>
        /// Applies the Quantum Fourier Transform (QFT) to the specified qubits.
        /// This algorithm rotates the state space to reveal periodicity —
        /// basically, it's a fancy way to turn time into frequency like a quantum DJ.
        /// </summary>
        public static void QuantumFourierTransform(QuantumSystem system, int[] qubits)
        {
            int n = qubits.Length;
            for (int i = 0; i < n; i++)
            {
                system.ApplySingleQubitGate(qubits[i], QuantumGates.Hadamard, "H");
                for (int j = i + 1; j < n; j++)
                {
                    double theta = Math.PI / Math.Pow(2, j - i + 1);
                    system.ApplyTwoQubitGate(qubits[j], qubits[i], QuantumGates.CPhase(theta), $"CPhase({theta})");
                }
            }

            // Swap qubits to reverse the order
            for (int i = 0; i < n / 2; i++)
            {
                system.ApplyTwoQubitGate(qubits[i], qubits[n - i - 1], QuantumGates.CNOT, "SWAP");
            }
        }

        /// <summary>
        /// Runs Grover’s search algorithm to find "the one" in an unsorted quantum haystack.
        /// The oracle defines which states are considered solutions,
        /// and the algorithm amplifies their probability like a quantum hype man.
        /// </summary>
        public static void GroverSearch(QuantumSystem system, int[] qubits, Func<int[], bool> oracle)
        {
            int n = qubits.Length;
            int iterations = (int)Math.Floor(Math.PI / 4 * Math.Sqrt(Math.Pow(2, n)));

            // Step 1: Create an equal superposition over all basis states.
            foreach (int qubit in qubits)
            {
                system.ApplySingleQubitGate(qubit, QuantumGates.Hadamard, "H");
            }

            // Repeat the Grover operator for "iterations" cycles.
            for (int i = 0; i < iterations; i++)
            {
                ApplyOracle(system, qubits, oracle);
                ApplyDiffusionOperator(system, qubits);
            }
        }

        /// <summary>
        /// Constructs and applies the oracle gate —
        /// it flips the sign (adds a -1 phase) of any state deemed "special" by the oracle function.
        /// This is like tagging certain quantum states as cursed, so Grover can sniff them out.
        /// </summary>
        private static void ApplyOracle(QuantumSystem system, int[] qubits, Func<int[], bool> oracle)
        {
            int n = qubits.Length;
            int dim = 1 << n;
            Complex[,] oracleMatrix = new Complex[dim, dim];
            for (int i = 0; i < dim; i++)
            {
                int[] basis = IndexToBits(i, n);
                // If the oracle marks the state, flip its phase.
                double factor = oracle(basis) ? -1.0 : 1.0;
                for (int j = 0; j < dim; j++)
                {
                    oracleMatrix[i, j] = (i == j) ? factor : 0;
                }
            }
            // Apply the constructed oracle unitary to the target qubits.
            system.ApplyMultiQubitGate(qubits, oracleMatrix, "Oracle");
        }

        /// <summary>
        /// Applies the Grover diffusion operator — a reflection over the mean amplitude.
        /// Think of it as quantum gaslighting: it makes the marked states stand out
        /// by subtly making everything else doubt its own existence.
        /// </summary>
        private static void ApplyDiffusionOperator(QuantumSystem system, int[] qubits)
        {
            int n = qubits.Length;
            int dim = 1 << n;

            // Hadamard on all qubits.
            foreach (int qubit in qubits)
            {
                system.ApplySingleQubitGate(qubit, QuantumGates.Hadamard, "H");
            }

            // Pauli-X on all qubits.
            foreach (int qubit in qubits)
            {
                system.ApplySingleQubitGate(qubit, QuantumGates.PauliX, "X");
            }

            // Multi-controlled Z gate: flip the phase of |0...0⟩.
            Complex[,] mcz = new Complex[dim, dim];
            for (int i = 0; i < dim; i++)
            {
                // Only state |0...0⟩ (i==0) gets a phase of –1.
                mcz[i, i] = (i == 0) ? -1.0 : 1.0;
            }
            system.ApplyMultiQubitGate(qubits, mcz, "MCZ");

            // Undo the previous X and H layers.
            foreach (int qubit in qubits)
            {
                system.ApplySingleQubitGate(qubit, QuantumGates.PauliX, "X");
            }
            foreach (int qubit in qubits)
            {
                system.ApplySingleQubitGate(qubit, QuantumGates.Hadamard, "H");
            }
        }

        #region Helper Methods

        /// <summary>
        /// Converts an integer into its binary representation as an array,
        /// with the most significant bit first. Useful for peeking behind the matrix.
        /// </summary>
        private static int[] IndexToBits(int index, int length)
        {
            int[] bits = new int[length];
            for (int i = length - 1; i >= 0; i--)
            {
                bits[i] = index & 1;
                index >>= 1;
            }
            return bits;
        }

        /// <summary>
        /// Reverses what IndexToBits did. Translates a binary array back into
        /// a good old-fashioned base-10 integer, just like grandma used to use.
        /// </summary>
        /// <param name="bits">An array of bits where the first element is the MSB.</param>
        /// <returns>The integer value represented by the bits.</returns>
        private static int BitsToIndex(int[] bits)
        {
            int index = 0;
            for (int i = 0; i < bits.Length; i++)
            {
                index = (index << 1) | bits[i];
            }
            return index;
        }

        /// <summary>
        /// Given a full system index, extracts the value of a subset of qubits as an integer.
        /// Handy when you want to zoom in on just a few bits of drama in a massive entangled soap opera.
        /// </summary>
        /// <param name="fullIndex">The integer representing the full quantum state.</param>
        /// <param name="targetQubits">
        /// An array of qubit positions to extract. These should correspond to the positions in the full state 
        /// (with 0 representing the most significant qubit).
        /// </param>
        /// <param name="totalQubits">The total number of qubits in the full state representation.</param>
        /// <returns>The integer value of the extracted substate.</returns>
        private static int ExtractSubstate(int fullIndex, int[] targetQubits, int totalQubits)
        {
            int subIndex = 0;
            foreach (int qubit in targetQubits)
            {
                // Extract the bit at the given qubit position.
                // Assumes that qubit 0 corresponds to the MSB in the full state.
                int bit = (fullIndex >> (totalQubits - 1 - qubit)) & 1;
                subIndex = (subIndex << 1) | bit;
            }
            return subIndex;
        }

        #endregion
    }

}

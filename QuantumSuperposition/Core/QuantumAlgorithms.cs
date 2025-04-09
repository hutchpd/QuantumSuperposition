using System;
using System.Numerics;
using QuantumSuperposition.Systems;

namespace QuantumSuperposition.Core
{

    public static class QuantumAlgorithms
    {
        /// <summary>
        /// Applies the Quantum Fourier Transform (QFT) to the specified qubits.
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
        /// Implements Grover’s search. The oracle is provided as a predicate over basis states.
        /// It builds the oracle (phase flip) unitary and the diffusion operator unitary,
        /// then applies them for an appropriate number of iterations.
        /// </summary>
        public static void GroverSearch(QuantumSystem system, int[] qubits, Func<int[], bool> oracle)
        {
            int n = qubits.Length;
            int iterations = (int)Math.Floor(Math.PI / 4 * Math.Sqrt(Math.Pow(2, n)));

            // Step 1: Create an equal superposition over all basis states.
            foreach (var qubit in qubits)
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
        /// Builds and applies the oracle unitary.  
        /// For each basis state in the subspace of the target qubits, if oracle(state)==true,
        /// that state is phased by –1; otherwise, it remains unchanged.
        /// </summary>
        private static void ApplyOracle(QuantumSystem system, int[] qubits, Func<int[], bool> oracle)
        {
            int n = qubits.Length;
            int dim = 1 << n;
            var oracleMatrix = new Complex[dim, dim];
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
        /// Builds and applies the Grover diffusion operator.
        /// </summary>
        private static void ApplyDiffusionOperator(QuantumSystem system, int[] qubits)
        {
            int n = qubits.Length;
            int dim = 1 << n;

            // Hadamard on all qubits.
            foreach (var qubit in qubits)
            {
                system.ApplySingleQubitGate(qubit, QuantumGates.Hadamard, "H");
            }

            // Pauli-X on all qubits.
            foreach (var qubit in qubits)
            {
                system.ApplySingleQubitGate(qubit, QuantumGates.PauliX, "X");
            }

            // Multi-controlled Z gate: flip the phase of |0...0⟩.
            var mcz = new Complex[dim, dim];
            for (int i = 0; i < dim; i++)
            {
                // Only state |0...0⟩ (i==0) gets a phase of –1.
                mcz[i, i] = (i == 0) ? -1.0 : 1.0;
            }
            system.ApplyMultiQubitGate(qubits, mcz, "MCZ");

            // Undo the previous X and H layers.
            foreach (var qubit in qubits)
            {
                system.ApplySingleQubitGate(qubit, QuantumGates.PauliX, "X");
            }
            foreach (var qubit in qubits)
            {
                system.ApplySingleQubitGate(qubit, QuantumGates.Hadamard, "H");
            }
        }

        #region Helper Methods

        /// <summary>
        /// Converts an integer index to a binary array (most-significant bit first) of given length.
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

        // TODO: add other helpers such as ExtractSubstate and BitsToIndex
        #endregion
    }

}

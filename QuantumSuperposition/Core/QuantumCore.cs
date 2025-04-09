using System.Numerics;
using QuantumSuperposition.Utilities;

namespace QuantumSuperposition.Core
{

    // Because quantum code should be at least as confusing as quantum physics.
    namespace QuantumCore
    {
        public class QuantumGate
        {
            public Complex[,] Matrix { get; }

            public QuantumGate(Complex[,] matrix)
            {
                // (Optionally) add validation to ensure 'matrix' is square or unitary.
                Matrix = matrix;
            }

            /// <summary>
            /// Returns a new QuantumGate which is the composition of this gate followed by the specified nextGate.
            /// (i.e. if applied to a state, the transformation is: nextGate.Matrix * this.Matrix * state)
            /// </summary>
            /// <param name="nextGate">The gate to apply after this gate.</param>
            /// <returns>A new QuantumGate representing the composite gate.</returns>
            public QuantumGate Then(QuantumGate nextGate)
            {
                // Ensure the matrices can be composed: the number of columns in this gate must match 
                // the number of rows in the nextGate.
                int thisRows = Matrix.GetLength(0);
                int thisCols = Matrix.GetLength(1);
                int nextRows = nextGate.Matrix.GetLength(0);
                int nextCols = nextGate.Matrix.GetLength(1);
                if (thisCols != nextRows)
                {
                    throw new InvalidOperationException("Cannot compose gates: dimensions do not match.");
                }

                var result = new Complex[nextRows, thisRows];
                // Compose in the proper order: if you have a state vector ψ,
                // applying this then nextGate gives: nextGate.Matrix * (this.Matrix * ψ) 
                // so the composite matrix is nextGate.Matrix * this.Matrix.
                for (int i = 0; i < nextRows; i++)
                {
                    for (int j = 0; j < thisRows; j++)
                    {
                        Complex sum = Complex.Zero;
                        for (int k = 0; k < thisCols; k++)
                        {
                            sum += nextGate.Matrix[i, k] * Matrix[k, j];
                        }
                        result[i, j] = sum;
                    }
                }

                return new QuantumGate(result);
            }

            // Allow implicit conversion from a Complex[,] to a QuantumGate for convenience.
            public static implicit operator QuantumGate(Complex[,] m) => new QuantumGate(m);

            // And an implicit conversion so QuantumGate can be used when a Complex[,] is expected.
            public static implicit operator Complex[,](QuantumGate gate) => gate.Matrix;
        }

        /// <summary>
        /// A centralized repository for common quantum gate matrices.
        /// This includes built-in gates like Hadamard, Root-NOT, etc.
        /// </summary>
        public static class QuantumGates
        {
            // Hadamard gate – creates an equal superposition.
            public static QuantumGate Hadamard => new QuantumGate(new Complex[,]
            {
                { 1/Math.Sqrt(2), 1/Math.Sqrt(2) },
                { 1/Math.Sqrt(2), -1/Math.Sqrt(2) }
            });

            /// <summary>
            /// Creates an RX gate with a given rotation angle theta.   
            /// </summary>
            /// <param name="theta"></param>
            /// <returns></returns>
            public static QuantumGate RX(double theta)
            {
                return new QuantumGate(new Complex[,] {
                    { Math.Cos(theta / 2), -Complex.ImaginaryOne * Math.Sin(theta / 2) },
                    { -Complex.ImaginaryOne * Math.Sin(theta / 2), Math.Cos(theta / 2) }
                });
            }

            /// <summary>
            /// Creates an RY gate with a given rotation angle theta.
            /// </summary>
            /// <param name="theta"></param>
            /// <returns></returns>
            public static QuantumGate CPhase(double theta)
            {
                return new QuantumGate(new Complex[,] {
                    { 1, 0, 0, 0 },
                    { 0, 1, 0, 0 },
                    { 0, 0, 1, 0 },
                    { 0, 0, 0, Complex.Exp(Complex.ImaginaryOne * theta) }
                });
            }



            // Identity gate – does nothing.
            public static Complex[,] Identity => new QuantumGate(new Complex[,]
            {
                { 1, 0 },
                { 0, 1 }
            });

            // Pauli-X gate (NOT gate) – flips the basis states.
            public static Complex[,] PauliX => new QuantumGate(new Complex[,]
            {
                { 0, 1 },
                { 1, 0 }
            });

            // Root-NOT gate – the square root of the Pauli-X gate.
            // When applied twice, it produces the Pauli-X gate.
            public static Complex[,] RootNot => new QuantumGate(new Complex[,]
            {
                { (1 + Complex.ImaginaryOne)/2.0, (1 - Complex.ImaginaryOne)/2.0 },
                { (1 - Complex.ImaginaryOne)/2.0, (1 + Complex.ImaginaryOne)/2.0 }
            });

            public static Complex[,] RootNotInverse => QuantumGateTools.InvertGate(RootNot);

            // =A phase gate that rotates by a given angle theta.
            public static Complex[,] Phase(double theta) => new QuantumGate(new Complex[,]
            {
                { 1, 0 },
                { 0, Complex.Exp(Complex.ImaginaryOne * theta) }
            });

            public static Complex[,] T => new QuantumGate(new Complex[,]
    {
            { 1, 0 },
            { 0, Complex.Exp(Complex.ImaginaryOne * Math.PI / 4) }
    });

            // CNOT gate (a 4×4 matrix)
            public static QuantumGate CNOT => new QuantumGate(new Complex[,]
            {
                {1, 0, 0, 0},
                {0, 1, 0, 0},
                {0, 0, 0, 1},
                {0, 0, 1, 0}
            });

            public static Complex[,] T_Dagger => QuantumGateTools.InvertGate(T);

        }
    }
}

using QuantumSuperposition.Utilities;
using System.Numerics;

namespace QuantumSuperposition.Core
{
    public class QuantumGate
    {
        public Complex[,] Matrix { get; }

        public QuantumGate(Complex[,] matrix)
        {
            Matrix = matrix;
        }

        public QuantumGate Then(QuantumGate nextGate)
        {
            int thisRows = Matrix.GetLength(0);
            int thisCols = Matrix.GetLength(1);
            int nextRows = nextGate.Matrix.GetLength(0);
            int nextCols = nextGate.Matrix.GetLength(1);
            if (thisCols != nextRows)
            {
                throw new InvalidOperationException("Cannot compose gates: dimensions do not match.");
            }

            Complex[,] result = new Complex[nextRows, thisRows];
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

        public static implicit operator QuantumGate(Complex[,] m) => new(m);
        public static implicit operator Complex[,](QuantumGate gate) => gate.Matrix;
    }

    public static class QuantumGates
    {
        // ---------- Single Qubit Standard Gates ----------
        public static QuantumGate Hadamard => new(new Complex[,]
        {
            { 1/Math.Sqrt(2), 1/Math.Sqrt(2) },
            { 1/Math.Sqrt(2), -1/Math.Sqrt(2) }
        });

        public static QuantumGate PauliX => new(new Complex[,]
        {
            {0,1},
            {1,0}
        });

        public static QuantumGate PauliY => new(new Complex[,]
        {
            {0, -Complex.ImaginaryOne},
            {Complex.ImaginaryOne, 0}
        });

        public static QuantumGate PauliZ => new(new Complex[,]
        {
            {1,0},
            {0,-1}
        });

        public static QuantumGate Phase(double theta) => new(new Complex[,]
        {
            {1,0},
            {0, Complex.Exp(Complex.ImaginaryOne * theta)}
        });

        public static QuantumGate PhaseShift(double theta) => Phase(theta); // alias for clarity

        public static QuantumGate T => Phase(Math.PI/4);
        public static QuantumGate T_Dagger => new(QuantumGateTools.InvertGate(T.Matrix));

        public static QuantumGate RX(double theta) => new(new Complex[,]
        {
            { Math.Cos(theta/2), -Complex.ImaginaryOne * Math.Sin(theta/2)},
            { -Complex.ImaginaryOne * Math.Sin(theta/2), Math.Cos(theta/2)}
        });

        // Root-NOT
        public static QuantumGate RootNot => new(new Complex[,]
        {
            { (1+Complex.ImaginaryOne)/2.0, (1-Complex.ImaginaryOne)/2.0 },
            { (1-Complex.ImaginaryOne)/2.0, (1+Complex.ImaginaryOne)/2.0 }
        });
        public static QuantumGate RootNotInverse => new(QuantumGateTools.InvertGate(RootNot.Matrix));

        // Identity (single qubit) and generalized identity
        public static QuantumGate Identity => new(new Complex[,]
        {
            {1,0},
            {0,1}
        });
        public static QuantumGate IdentityOfLength(int qubits)
        {
            if (qubits < 1) throw new ArgumentOutOfRangeException(nameof(qubits));
            int dim = 1 << qubits;
            Complex[,] m = new Complex[dim, dim];
            for (int i=0;i<dim;i++) m[i,i] = Complex.One;
            return new QuantumGate(m);
        }

        // Hadamard of length n (tensor product H ⊗ ... ⊗ H)
        public static QuantumGate HadamardOfLength(int qubits)
        {
            if (qubits < 1) throw new ArgumentOutOfRangeException(nameof(qubits));
            Complex[,] current = Hadamard.Matrix;
            for (int i=1;i<qubits;i++)
            {
                current = Tensor(current, Hadamard.Matrix);
            }
            return new QuantumGate(current);
        }

        // Tensor product utility for gates
        private static Complex[,] Tensor(Complex[,] a, Complex[,] b)
        {
            int aRows = a.GetLength(0); int aCols = a.GetLength(1);
            int bRows = b.GetLength(0); int bCols = b.GetLength(1);
            Complex[,] result = new Complex[aRows*bRows, aCols*bCols];
            for (int i=0;i<aRows;i++)
            {
                for (int j=0;j<aCols;j++)
                {
                    for (int k=0;k<bRows;k++)
                    {
                        for (int l=0;l<bCols;l++)
                        {
                            result[i*bRows + k, j*bCols + l] = a[i,j] * b[k,l];
                        }
                    }
                }
            }
            return result;
        }

        // CNOT gate
        public static QuantumGate CNOT => new(new Complex[,]
        {
            {1,0,0,0},
            {0,1,0,0},
            {0,0,0,1},
            {0,0,1,0}
        });

        // SWAP gate (two qubits)
        public static QuantumGate SWAP => new(new Complex[,]
        {
            {1,0,0,0},
            {0,0,1,0},
            {0,1,0,0},
            {0,0,0,1}
        });

        // Square root of SWAP gate
        public static QuantumGate SquareRootSwap => new(new Complex[,]
        {
            {1,0,0,0},
            {0, 0.5 + 0.5*Complex.ImaginaryOne, 0.5 - 0.5*Complex.ImaginaryOne, 0},
            {0, 0.5 - 0.5*Complex.ImaginaryOne, 0.5 + 0.5*Complex.ImaginaryOne, 0},
            {0,0,0,1}
        });

        // Controlled version of any gate: block matrix diag(I, inner)
        public static QuantumGate Controlled(QuantumGate inner)
        {
            int dim = inner.Matrix.GetLength(0);
            if (dim != inner.Matrix.GetLength(1)) throw new ArgumentException("Inner gate must be square.");
            Complex[,] controlled = new Complex[dim*2, dim*2];
            // Top-left identity
            for (int i=0;i<dim;i++) controlled[i,i] = Complex.One;
            // Bottom-right inner
            for (int i=0;i<dim;i++)
            {
                for (int j=0;j<dim;j++)
                {
                    controlled[dim + i, dim + j] = inner.Matrix[i,j];
                }
            }
            return new QuantumGate(controlled);
        }

        // Toffoli (CCNOT) 3-qubit gate
        public static QuantumGate Toffoli => new(CreateToffoli());
        private static Complex[,] CreateToffoli()
        {
            Complex[,] m = new Complex[8,8];
            for (int i=0;i<8;i++) m[i,i] = Complex.One; // start identity
            // Flip target (bit2) when bit0 and bit1 are 1 => states 6 (110) & 7 (111)
            // Need to swap |110> (6) with |111> (7)
            m[6,6] = 0; m[7,7] = 0; // clear original diag
            m[6,7] = Complex.One; // |111> -> |110>
            m[7,6] = Complex.One; // |110> -> |111>
            return m;
        }

        // Fredkin (CSWAP) gate (control: qubit0, swap qubit1 & qubit2)
        public static QuantumGate Fredkin => new(CreateFredkin());
        private static Complex[,] CreateFredkin()
        {
            Complex[,] m = new Complex[8,8];
            for (int i=0;i<8;i++) m[i,i] = Complex.One;
            // When control (bit0) = 1, swap bits1 and bits2: states where high bit=1 and remaining bits differ.
            // Basis ordering assumed |abc> with a=bit0 (MSB).
            // States 4 (100) and 4 unaffected? For combinations: 5 (101) <-> 6 (110)
            m[5,5] = 0; m[6,6] = 0; // clear
            m[5,6] = Complex.One; // 110 -> 101
            m[6,5] = Complex.One; // 101 -> 110
            return m;
        }

        // Controlled-Phase for two qubits (already had CPhase)
        public static QuantumGate CPhase(double theta) => new(new Complex[,]
        {
            {1,0,0,0},
            {0,1,0,0},
            {0,0,1,0},
            {0,0,0, Complex.Exp(Complex.ImaginaryOne*theta)}
        });

        // Quantum Fourier Transform gate builder over 'registerLength' qubits.
        public static QuantumGate QuantumFourierTransformGate(int registerLength)
        {
            if (registerLength < 1) throw new ArgumentOutOfRangeException(nameof(registerLength));
            int N = 1 << registerLength; // dimension
            Complex[,] m = new Complex[N,N];
            double norm = 1.0 / Math.Sqrt(N);
            for (int k=0;k<N;k++)
            {
                for (int l=0;l<N;l++)
                {
                    double angle = 2 * Math.PI * k * l / N;
                    m[k,l] = norm * Complex.Exp(Complex.ImaginaryOne * angle);
                }
            }
            return new QuantumGate(m);
        }
    }
}

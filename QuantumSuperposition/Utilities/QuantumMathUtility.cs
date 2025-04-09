using System.Numerics;
using QuantumSuperposition.QuantumSoup;

namespace QuantumSuperposition.Utilities
{
    /// <summary>
    /// Utility for combining values. Think of it like a quantum kitchen mixer.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static class QuantumMathUtility<T>
    {
        public static IEnumerable<T> CombineAll(IEnumerable<T> a, IEnumerable<T> b, Func<T, T, T> op) =>
            a.SelectMany(x => b.Select(y => op(x, y)));

        public static IEnumerable<T> Combine(IEnumerable<T> a, T b, Func<T, T, T> op) =>
            a.Select(x => op(x, b));

        public static IEnumerable<T> Combine(T a, IEnumerable<T> b, Func<T, T, T> op) =>
            b.Select(x => op(a, x));

        /// <summary>
        /// Applies a matrix to a vector, returning the resulting vector.
        /// </summary>
        /// <param name="vector"></param>
        /// <param name="matrix"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static Complex[] ApplyMatrix(Complex[] vector, Complex[,] matrix)
        {
            int dim = vector.Length;

            if (matrix.GetLength(0) != dim || matrix.GetLength(1) != dim)
                throw new ArgumentException($"Matrix must be square with size {dim}×{dim}.");

            var result = new Complex[dim];
            for (int i = 0; i < dim; i++)
            {
                result[i] = Complex.Zero;
                for (int j = 0; j < dim; j++)
                {
                    result[i] += matrix[i, j] * vector[j];
                }
            }

            return result;
        }

        /// <summary>
        /// Applies a quantum gate (unitary matrix) to a state vector.
        /// </summary>
        /// <param name="vector"></param>
        /// <param name="gate"></param>
        /// <returns></returns>
        public static Complex[] ApplyGate(Complex[] vector, Complex[,] gate)
        => ApplyMatrix(vector, gate);

        /// <summary>
        /// Computes the tensor product of multiple QuBits, returning a dictionary of state combinations with combined amplitudes.
        /// </summary>
        public static Dictionary<T[], Complex> TensorProduct<T>(params QuBit<T>[] qubits)
        {
            if (qubits == null || qubits.Length == 0)
                throw new ArgumentException("At least one qubit must be provided.");

            // Start with a single empty state with amplitude 1
            var result = new List<(List<T> state, Complex amplitude)>
        {
            (new List<T>(), Complex.One)
        };

            foreach (var qubit in qubits)
            {
                var newResult = new List<(List<T>, Complex)>();
                foreach (var (prefix, amp) in result)
                {
                    foreach (var (value, weight) in qubit.ToWeightedValues())
                    {
                        var newState = new List<T>(prefix) { value };
                        newResult.Add((newState, amp * weight));
                    }
                }
                result = newResult;
            }

            // Convert to final dictionary
            var dict = new Dictionary<T[], Complex>(new TensorKeyComparer<T>());
            foreach (var (state, amp) in result)
            {
                dict[state.ToArray()] = amp;
            }

            return dict;
        }

        // A comparer for arrays of T
        private class TensorKeyComparer<TK> : IEqualityComparer<TK[]>
        {
            public bool Equals(TK[]? x, TK[]? y)
            {
                if (x == null || y == null) return false;
                return x.SequenceEqual(y);
            }

            public int GetHashCode(TK[] obj)
            {
                unchecked
                {
                    int hash = 17;
                    foreach (var item in obj)
                    {
                        hash = hash * 31 + (item?.GetHashCode() ?? 0);
                    }
                    return hash;
                }
            }
        }


    }
}

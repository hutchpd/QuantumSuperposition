using System.Numerics;

namespace QuantumSuperposition.Utilities
{

    public static class QuantumGateTools
    {
        public static Complex[,] ConjugateTranspose(Complex[,] matrix)
        {
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);
            var result = new Complex[cols, rows]; // transpose shape

            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                    result[j, i] = Complex.Conjugate(matrix[i, j]);

            return result;
        }

        public static Complex[,] InvertGate(Complex[,] gate)
        {
            // Unitary matrix inverse = its conjugate transpose
            return ConjugateTranspose(gate);
        }
    }
}